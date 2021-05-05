namespace Microsoft.InnerEye.Listener.Common.Providers
{
    using System;
    using System.Collections.Generic;
    using System.IO;
    using Microsoft.Extensions.Logging;
    using Microsoft.InnerEye.Gateway.Logging;
    using Newtonsoft.Json;
    using Newtonsoft.Json.Converters;

    /// <summary>
    /// Class that monitors a JSON settings file or folder.
    /// </summary>
    /// <typeparam name="T">Data type underlying the JSON settings.</typeparam>
    public class BaseConfigProvider<T> : IDisposable
    {
        /// <summary>
        /// Logger for errors loading or parsing JSON.
        /// </summary>
        private readonly ILogger _logger;

        /// <summary>
        /// JSON file or folder name.
        /// </summary>
        private readonly string _settingsFileOrFolderName;

        /// <summary>
        /// Optional flat map to handle folders.
        /// </summary>
        private readonly Func<IEnumerable<T>, T> _flatMap;

        /// <summary>
        /// File system watcher to monitor changes to file or folder.
        /// </summary>
        private readonly FileSystemWatcher _fileSystemWatcher;

        private bool disposedValue;

        /// <summary>
        /// Config as last loaded from file or folder.
        /// </summary>
        public T Config { get; protected set; }

        /// <summary>
        /// Called when the config has changed.
        /// </summary>
        public event EventHandler ConfigChanged;

        /// <summary>
        /// Initialize a new instance of the <see cref="BaseConfigProvider"/> class.
        /// </summary>
        /// <param name="logger">Logger.</param>
        /// <param name="folderName">Settings folder name.</param>
        /// <param name="settingsFile">Optional settings file.</param>
        /// <param name="flatMap">Optional flat map to handle folders.</param>
        public BaseConfigProvider(
            ILogger logger,
            string folderName,
            string settingsFile,
            Func<IEnumerable<T>, T> flatMap = null)
        {
            _logger = logger;
            _settingsFileOrFolderName = Path.Combine(folderName, settingsFile);
            _flatMap = flatMap;

            _fileSystemWatcher = new FileSystemWatcher(folderName)
            {
                Filter = File.Exists(_settingsFileOrFolderName) ? settingsFile : "*.json",
                NotifyFilter = NotifyFilters.LastWrite,
            };

            _fileSystemWatcher.Changed += OnChanged;
            _fileSystemWatcher.EnableRaisingEvents = true;

            Load(false);
        }

        /// <summary>
        /// File watcher Changed event handler. Filter the events and call ConfigChanged.
        /// </summary>
        /// <param name="sender">Sender.</param>
        /// <param name="e">File system event args.</param>
        private void OnChanged(object sender, FileSystemEventArgs e)
        {
            if (e.ChangeType != WatcherChangeTypes.Changed)
            {
                return;
            }

            var logEntry = LogEntry.Create(ServiceStatus.NewConfigurationDetetected,
                string.Format("Settings have changed: {0}", e.FullPath));
            logEntry.Log(_logger, LogLevel.Information);

            Load(true);
        }

        /// <summary>
        /// Load/reload config files.
        /// </summary>
        /// <param name="reload">True if reloading, false if loading.</param>
        public void Load(bool reload)
        {
            var (t, loaded, ts) = Load();

            if (!loaded && ts == null)
            {
                return;
            }

            Config = _flatMap != null && ts != null ? _flatMap(ts) : t;

            if (reload)
            {
                ConfigChanged?.Invoke(this, new EventArgs());
            }
        }

        /// <summary>
        /// Load T or Ts from a JSON file or folder.
        /// </summary>
        /// <returns>Tuple of: T, if loading a single file, bool if single file loaded successfully, Ts if loading a folder.</returns>
        private (T, bool, IEnumerable<T>) Load()
        {
            if (File.Exists(_settingsFileOrFolderName))
            {
                var (t, loaded, parsed) = LoadFile(_settingsFileOrFolderName);

                return (t, loaded && parsed, null);
            }
            else if (Directory.Exists(_settingsFileOrFolderName))
            {
                var ts = new List<T>();

                foreach (var file in Directory.EnumerateFiles(_settingsFileOrFolderName, "*.json"))
                {
                    var (t, loaded, parsed) = LoadFile(file);
                    if (!loaded)
                    {
                        // File still in use, FileWatcher has reported file changed but
                        // the other process has not finished yet.
                        return (default(T), false, null);
                    }

                    if (parsed)
                    {
                        ts.Add(t);
                    }
                }

                return (default(T), false, ts.ToArray());
            }
            else
            {
                var logEntry = LogEntry.Create(ServiceStatus.NewConfigurationError,
                    string.Format("Settings is neither a file nor a folder: {0}", _settingsFileOrFolderName));
                logEntry.Log(_logger, LogLevel.Error);

                return (default(T), false, null);
            }
        }

        /// <summary>
        /// Update settings file, according to an update callback function.
        /// </summary>
        /// <param name="updater">Callback to update the settings. Return new settings for update, or the same object to not update.</param>
        /// <param name="equalityComparer">Optional, how to compare objects.</param>
        public (T, bool) Update(Func<T, T> updater, IEqualityComparer<T> equalityComparer = null)
        {
            if (!File.Exists(_settingsFileOrFolderName))
            {
                throw new NotImplementedException(string.Format("Can only update single settings files: {0}", _settingsFileOrFolderName));
            }

            var (t, loaded, parsed) = LoadFile(_settingsFileOrFolderName);
            if (!loaded || !parsed)
            {
                return (default(T), false);
            }

            var newt = updater.Invoke(t);

            equalityComparer = equalityComparer ?? EqualityComparer<T>.Default;

            if (equalityComparer.Equals(newt, t))
            {
                return (default(T), false);
            }

            SaveFile(newt, _settingsFileOrFolderName);

            Config = newt;

            return (newt, true);
        }

        /// <summary>
        /// Load T from a JSON file.
        /// </summary>
        /// <param name="path">Path to file.</param>
        /// <returns>Triple of T, true if file loaded correctly, false otherwise, true if file parsed correctly, false otherwise.</returns>
        private (T, bool, bool) LoadFile(string path)
        {
            try
            {
                var jsonText = File.ReadAllText(path);

                return (JsonConvert.DeserializeObject<T>(jsonText), true, true);
            }
            catch (JsonSerializationException e)
            {
                var logEntry = LogEntry.Create(ServiceStatus.NewConfigurationError,
                    string.Format("Unable to parse settings file {0}", path));
                logEntry.Log(_logger, LogLevel.Error, e);

                return (default(T), true, false);
            }
            catch (IOException e)
            {
                var logEntry = LogEntry.Create(ServiceStatus.NewConfigurationError,
                    string.Format("Unable to load settings file {0}", path));
                logEntry.Log(_logger, LogLevel.Error, e);

                return (default(T), false, false);
            }
        }

        /// <summary>
        /// Save a T to a JSON file.
        /// </summary>
        /// <param name="t">Instance of type T.</param>
        /// <param name="path">Path to file.</param>
        private static void SaveFile(T t, string path)
        {
            var serializerSettings = new JsonSerializerSettings()
            {
                Converters = new[] { new StringEnumConverter() },
                Formatting = Formatting.Indented,
                NullValueHandling = NullValueHandling.Include
            };

            var jsonText = JsonConvert.SerializeObject(t, serializerSettings);
            File.WriteAllText(path, jsonText);
        }

        /// <summary>
        /// Disposes of all managed resources.
        /// </summary>
        /// <param name="disposing">If we are disposing.</param>
        protected virtual void Dispose(bool disposing)
        {
            if (disposedValue)
            {
                return;
            }

            if (disposing)
            {
                _fileSystemWatcher.Dispose();
            }

            disposedValue = true;
        }

        /// <summary>
        /// Implements the disposable pattern.
        /// </summary>
        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }
    }
}
