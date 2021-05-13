namespace Microsoft.InnerEye.Listener.Common.Providers
{
    using System;
    using System.Collections.Generic;
    using System.Globalization;
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
        /// Class to hold results from trying to open and parse a possible JSON file.
        /// </summary>
        private class LoadJsonResult
        {
            /// <summary>
            /// True if the file has loaded, false otherwise.
            /// </summary>
            /// <remarks>
            /// The FileSystemWatcher can report file changed events whilst another process is saving
            /// a file. In this case there may be a System.IO.IOException (“File used by another process”).
            /// </remarks>
            public bool Loaded { get; }

            /// <summary>
            /// True if the file has been parsed, false otherwise.
            /// </summary>
            /// <remarks>
            /// In the case of loading from a folder, all files are loaded and parsed. There may be other files
            /// there and they are to be ignored.
            /// </remarks>
            public bool Parsed { get; }

            /// <summary>
            /// An instance of type T if the file has been loaded and parsed correctly. Default(T) otherwise.
            /// </summary>
            public T Result { get; }

            /// <summary>
            /// Initialize a new instance of the <see cref="LoadJsonResult"/> class.
            /// </summary>
            /// <param name="loaded">True if file loaded.</param>
            /// <param name="parsed">True if file parsed.</param>
            /// <param name="result">Instance of type T if loaded and parsed.</param>
            public LoadJsonResult(bool loaded, bool parsed, T result)
            {
                Loaded = loaded;
                Parsed = parsed;
                Result = result;
            }
        }

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

        /// <summary>
        /// Disposed flag for IDisposable.
        /// </summary>
        private bool disposedValue;

        /// <summary>
        /// Config as last loaded from file or folder.
        /// </summary>
        public T Config { get; private set; }

        /// <summary>
        /// Called when the config has changed.
        /// </summary>
        public event EventHandler ConfigChanged;

        /// <summary>
        /// Initialize a new instance of the <see cref="BaseConfigProvider"/> class.
        /// </summary>
        /// <param name="logger">Logger.</param>
        /// <param name="folderName">Settings folder name.</param>
        /// <param name="settingsFile">Optional settings file, use String.Empty to monitor a folder.</param>
        /// <param name="flatMap">Optional flat map to handle folders. This should merge a T from each file in the folder
        /// into a single new T. This is required if monitoring a folder.</param>
        public BaseConfigProvider(
            ILogger logger,
            string folderName,
            string settingsFile,
            Func<IEnumerable<T>, T> flatMap = null)
        {
            _logger = logger;
            _settingsFileOrFolderName = Path.Combine(folderName, settingsFile);
            _flatMap = flatMap;

            if (string.IsNullOrWhiteSpace(settingsFile) && flatMap == null)
            {
                throw new ArgumentNullException(nameof(flatMap), "If monitoring a folder, flatMap must be supplied");
            }

            _fileSystemWatcher = new FileSystemWatcher(folderName)
            {
                Filter = !string.IsNullOrWhiteSpace(settingsFile) ? settingsFile : "*.json",
                NotifyFilter = NotifyFilters.LastWrite,
            };

            _fileSystemWatcher.Changed += OnChanged;
            _fileSystemWatcher.EnableRaisingEvents = true;

            Load();
        }

        /// <summary>
        /// File watcher Changed event handler. Filter the events, reload the config and if successful invoke ConfigChanged.
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
                string.Format(CultureInfo.InvariantCulture, "Settings have changed: {0}", e.FullPath));
            logEntry.Log(_logger, LogLevel.Information);

            if (!Load())
            {
                return;
            }

            ConfigChanged?.Invoke(this, new EventArgs());
        }

        /// <summary>
        /// Load T from a JSON file or folder.
        /// </summary>
        /// <returns>True if new config has been loaded, false otherwise.</returns>
        private bool Load()
        {
            if (File.Exists(_settingsFileOrFolderName))
            {
                var loadJsonResult = LoadFile(_settingsFileOrFolderName);

                if (!loadJsonResult.Loaded || !loadJsonResult.Parsed)
                {
                    return false;
                }

                Config = loadJsonResult.Result;

                return true;
            }
            else if (Directory.Exists(_settingsFileOrFolderName))
            {
                var ts = new List<T>();

                foreach (var file in Directory.EnumerateFiles(_settingsFileOrFolderName, "*.json"))
                {
                    var loadJsonResult = LoadFile(file);
                    if (!loadJsonResult.Loaded)
                    {
                        // File still in use, FileWatcher has reported file changed but
                        // the other process has not finished yet.
                        return false;
                    }

                    if (loadJsonResult.Parsed)
                    {
                        ts.Add(loadJsonResult.Result);
                    }
                }

                Config = _flatMap(ts);

                return true;
            }
            else
            {
                var logEntry = LogEntry.Create(ServiceStatus.NewConfigurationError,
                    string.Format(CultureInfo.InvariantCulture, "Settings is neither a file nor a folder: {0}", _settingsFileOrFolderName));
                logEntry.Log(_logger, LogLevel.Error);

                return false;
            }
        }

        /// <summary>
        /// Update settings file, according to an update callback function.
        /// </summary>
        /// <param name="updater">Callback to update the settings. Return new settings for update, or the same object to not update.</param>
        /// <param name="equalityComparer">Optional, how to compare objects.</param>
        public (T, bool) Update(Func<T, T> updater, IEqualityComparer<T> equalityComparer = null)
        {
            updater = updater ?? throw new ArgumentNullException(nameof(updater));

            if (!File.Exists(_settingsFileOrFolderName))
            {
                throw new NotImplementedException(string.Format(CultureInfo.InvariantCulture, "Can only update single settings files: {0}", _settingsFileOrFolderName));
            }

            var loadJsonResult = LoadFile(_settingsFileOrFolderName);
            if (!loadJsonResult.Loaded || !loadJsonResult.Parsed)
            {
                return (default(T), false);
            }

            var newt = updater(loadJsonResult.Result);

            equalityComparer = equalityComparer ?? EqualityComparer<T>.Default;

            if (equalityComparer.Equals(newt, loadJsonResult.Result))
            {
                return (default(T), false);
            }

            SaveFile(newt, _settingsFileOrFolderName);

            return (newt, true);
        }

        /// <summary>
        /// Load T from a JSON file.
        /// </summary>
        /// <param name="path">Path to file.</param>
        /// <returns>New <see cref="LoadJsonResult"/>.</returns>
        private LoadJsonResult LoadFile(string path)
        {
            try
            {
                var jsonText = File.ReadAllText(path);

                return new LoadJsonResult(true, true, JsonConvert.DeserializeObject<T>(jsonText));
            }
            catch (JsonSerializationException e)
            {
                var logEntry = LogEntry.Create(ServiceStatus.NewConfigurationError,
                    string.Format(CultureInfo.InvariantCulture, "Unable to parse settings file {0}", path));
                logEntry.Log(_logger, LogLevel.Error, e);

                return new LoadJsonResult(true, false, default(T));
            }
            catch (IOException e)
            {
                var logEntry = LogEntry.Create(ServiceStatus.NewConfigurationError,
                    string.Format(CultureInfo.InvariantCulture, "Unable to load settings file {0}", path));
                logEntry.Log(_logger, LogLevel.Error, e);

                return new LoadJsonResult(false, false, default(T));
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
