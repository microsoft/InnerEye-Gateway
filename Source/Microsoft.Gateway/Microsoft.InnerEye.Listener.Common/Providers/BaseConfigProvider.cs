namespace Microsoft.InnerEye.Listener.Common.Providers
{
    using System;
    using System.Collections.Generic;
    using System.IO;
    using Microsoft.Extensions.Logging;
    using Microsoft.InnerEye.Gateway.Logging;
    using Newtonsoft.Json;

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
        protected T _t;

        /// <summary>
        /// Cached copy of data as last loaded from folder of JSON files.
        /// </summary>
        protected IEnumerable<T> _ts;

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
                _ts = null;

                (_t, _) = LoadFile(_settingsFileOrFolderName);
            }
            else if (Directory.Exists(_settingsFileOrFolderName))
            {
                _t = default(T);
                var ts = new List<T>();

                foreach (var file in Directory.EnumerateFiles(_settingsFileOrFolderName, "*.json"))
                {
                    var (t, loaded) = LoadFile(file);
                    if (loaded)
                    {
                        ts.Add(t);
                    }
                }

                _ts = ts.ToArray();
            }
            else
            {
                var logEntry = LogEntry.Create(ServiceStatus.NewConfigurationError,
                    string.Format("Settings is neither a file nor a folder: {0}", _settingsFileOrFolderName));
                logEntry.Log(_logger, LogLevel.Error);
            }
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
    }
}
