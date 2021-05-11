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
        /// Cached copy of data as last loaded from JSON file.
        /// </summary>
        protected T Result { get; set; }

        /// <summary>
        /// Cached copy of data as last loaded from folder of JSON files.
        /// </summary>
        protected IEnumerable<T> Ts { get; set; }

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
        protected void Load()
        {
            if (File.Exists(_settingsFileOrFolderName))
            {
                Ts = null;

                (Result, _) = LoadFile(_settingsFileOrFolderName);
            }
            else if (Directory.Exists(_settingsFileOrFolderName))
            {
                Result = default(T);
                var ts = new List<T>();

                foreach (var file in Directory.EnumerateFiles(_settingsFileOrFolderName, "*.json"))
                {
                    var (t, loaded) = LoadFile(file);
                    if (loaded)
                    {
                        ts.Add(t);
                    }
                }

                Ts = ts.ToArray();
            }
            else
            {
                var logEntry = LogEntry.Create(ServiceStatus.NewConfigurationError,
                    string.Format(CultureInfo.InvariantCulture, "Settings is neither a file nor a folder: {0}", _settingsFileOrFolderName));
                logEntry.Log(_logger, LogLevel.Error);
            }
        }

        /// <summary>
        /// Update settings file, according to an update callback function.
        /// </summary>
        /// <param name="updater">Callback to update the settings. Return new settings for update, or the same object to not update.</param>
        /// <param name="equalityComparer">How to compare objects.</param>
        protected void UpdateFile(Func<T, T> updater, IEqualityComparer<T> equalityComparer)
        {
            updater = updater ?? throw new ArgumentNullException(nameof(updater));

            if (!File.Exists(_settingsFileOrFolderName))
            {
                throw new NotImplementedException(string.Format(CultureInfo.InvariantCulture, "Can only update single settings files: {0}", _settingsFileOrFolderName));
            }

            var (t, loaded) = LoadFile(_settingsFileOrFolderName);
            if (!loaded)
            {
                return;
            }

            var newt = updater.Invoke(t);
            equalityComparer = equalityComparer ?? EqualityComparer<T>.Default;
            if (equalityComparer.Equals(newt, t))
            {
                return;
            }

            SaveFile(newt, _settingsFileOrFolderName);
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
            catch (JsonSerializationException e)
            {
                var logEntry = LogEntry.Create(ServiceStatus.NewConfigurationError,
                    string.Format(CultureInfo.InvariantCulture, "Unable to parse settings file {0}", path));
                logEntry.Log(_logger, LogLevel.Error, e);

                return (default(T), false);
            }
            catch (IOException e)
            {
                var logEntry = LogEntry.Create(ServiceStatus.NewConfigurationError,
                    string.Format(CultureInfo.InvariantCulture, "Unable to load settings file {0}", path));
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
