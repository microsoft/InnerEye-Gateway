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
    public class BaseConfigProvider<T>
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
        /// Initialize a new instance of the <see cref="BaseConfigProvider"/> class.
        /// </summary>
        /// <param name="logger">Logger.</param>
        /// <param name="settingsFileOrFolderName">JSON settings file or folder.</param>
        public BaseConfigProvider(
            ILogger logger,
            string settingsFileOrFolderName)
        {
            _logger = logger;
            _settingsFileOrFolderName = settingsFileOrFolderName;
        }

        /// <summary>
        /// Load T or Ts from a JSON file or folder.
        /// </summary>
        /// <returns>Tuple of: T, if loading a single file, bool if single file loaded successfully, Ts if loading a folder.</returns>
        protected (T, bool, IEnumerable<T>) Load()
        {
            if (File.Exists(_settingsFileOrFolderName))
            {
                var (t, loaded) = LoadFile(_settingsFileOrFolderName);

                return (t, loaded, null);
            }
            else if (Directory.Exists(_settingsFileOrFolderName))
            {
                var ts = new List<T>();

                foreach (var file in Directory.EnumerateFiles(_settingsFileOrFolderName, "*.json"))
                {
                    var (t, loaded) = LoadFile(file);
                    if (loaded)
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
        /// <param name="equalityComparer">How to compare objects.</param>
        protected (T, bool) UpdateFile(Func<T, T> updater, IEqualityComparer<T> equalityComparer)
        {
            if (!File.Exists(_settingsFileOrFolderName))
            {
                throw new NotImplementedException(string.Format("Can only update single settings files: {0}", _settingsFileOrFolderName));
            }

            var (t, loaded) = LoadFile(_settingsFileOrFolderName);
            if (!loaded)
            {
                return (default(T), false);
            }

            var newt = updater.Invoke(t);
            if (equalityComparer.Equals(newt, t))
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
        /// <returns>Pair of T and true if file loaded correctly, false otherwise.</returns>
        private (T, bool) LoadFile(string path)
        {
            try
            {
                var jsonText = File.ReadAllText(path);

                return (JsonConvert.DeserializeObject<T>(jsonText), true);
            }
            catch (Exception e)
            {
                var logEntry = LogEntry.Create(ServiceStatus.NewConfigurationError,
                    string.Format("Unable to load settings file {0}", path));
                logEntry.Log(_logger, LogLevel.Error, e);

                return (default(T), false);
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
    }
}
