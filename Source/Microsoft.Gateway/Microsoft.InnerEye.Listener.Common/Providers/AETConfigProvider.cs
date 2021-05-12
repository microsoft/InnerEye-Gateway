namespace Microsoft.InnerEye.Listener.Common.Providers
{
    using System.Collections.Generic;
    using System.IO;
    using System.Linq;
    using Microsoft.Extensions.Logging;

    /// <summary>
    /// Monitor a JSON file or folder containing a list of <see cref="AETConfigModel"/>.
    /// </summary>
    public class AETConfigProvider : BaseConfigProvider<IEnumerable<AETConfigModel>>
    {
        /// <summary>
        /// File name for JSON file containing a list of <see cref="AETConfigModel"/>.
        /// </summary>
        public static readonly string AETConfigFileName = "GatewayModelRulesConfig.json";

        /// <summary>
        /// Folder name for folder containing JSON files, each containing a list of <see cref="AETConfigModel"/>.
        /// </summary>
        public static readonly string AETConfigFolderName = "GatewayModelRulesConfig";

        /// <summary>
        /// Initialize a new instance of the <see cref="AETConfigProvider"/> class.
        /// </summary>
        /// <param name="logger">Logger.</param>
        /// <param name="configurationsPathRoot">Path to folder containing AETConfigFileName.</param>
        /// <param name="useFile">True to use config file, false to use folder of files.</param>
        public AETConfigProvider(
            ILogger logger,
            string configurationsPathRoot,
            bool useFile = false) : base(logger,
            Path.Combine(configurationsPathRoot, useFile ? string.Empty : AETConfigFolderName),
            useFile ? AETConfigFileName : string.Empty,
            MergeModels)
        {
        }

        /// <summary>
        /// Helper to create a <see cref="Func{TResult}"/> for returning list of <see cref="AETConfigModel"/> from cache.
        /// </summary>
        /// <returns>Cached list of <see cref="AETConfigModel"/>.</returns>
        public IEnumerable<AETConfigModel> AETConfigModels() =>
            Config;

        /// <summary>
        /// Merge a list of lists of AET config models into one list.
        /// </summary>
        /// <remarks>
        /// Merging is only handled in one place. The algorithm is:
        /// Create a new empty output list of AET config model.
        /// For each input AET config model:
        ///     Try to find an existing AET config model in the output list with this Called and Calling AET.
        ///     If an existing AET config model cannot be found then copy this to the output.
        ///     Otherwise: append the list of ModelsConfig to the existing AET config model, ignoring all other properties,
        ///         and replace it in the output list.
        /// </remarks>
        /// <param name="modelLists">List of lists of AET config models.</param>
        /// <returns>List of AET config models.</returns>
        private static List<AETConfigModel> MergeModels(IEnumerable<IEnumerable<AETConfigModel>> modelLists)
        {
            var mergedModels = new List<AETConfigModel>();

            foreach (var modelList in modelLists)
            {
                foreach (var model in modelList)
                {
                    var match = ApplyAETModelConfigProvider.GetAETConfigModel(mergedModels, model.CalledAET, model.CallingAET);

                    if (match != null)
                    {
                        var mergedModel = match.With(
                            aetConfig: match.AETConfig.With(
                                config: match.AETConfig.Config.With(
                                    modelsConfig: match.AETConfig.Config.ModelsConfig.Concat(model.AETConfig.Config.ModelsConfig).ToArray())));

                        mergedModels.Remove(match);
                        mergedModels.Add(mergedModel);
                    }
                    else
                    {
                        mergedModels.Add(model);
                    }
                }
            }

            return mergedModels;
        }
    }
}
