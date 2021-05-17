// Copyright (c) Microsoft Corporation.
// Licensed under the MIT license.

namespace Microsoft.InnerEye.Listener.Common
{
    using System;

    using Microsoft.InnerEye.Azure.Segmentation.API.Common;

    /// <summary>
    /// The dry run folders.
    /// </summary>
    public static class DryRunFolders
    {
        /// <summary>
        /// The dry run feedback folder
        /// </summary>
        public const string DryRunAnonymisedFeedbackFolder = "DryRunFeedbackAnonymized";

        /// <summary>
        /// The dry run model folder
        /// </summary>
        public const string DryRunAnonymisedImageFolder = "DryRunModelAnonymizedImage";

        /// <summary>
        /// The dry run model with result folder.
        /// </summary>
        public const string DryRunModelWithResultFolder = "DryRunRTResultDeAnonymized";

        /// <summary>
        /// Converts the config type to a dry run folder.
        /// </summary>
        /// <param name="configType">Type of the configuration.</param>
        /// <returns>The dry run folder.</returns>
        /// <exception cref="ArgumentException">If the config type is not a dry run type.</exception>
        public static string GetFolder(AETConfigType configType)
        {
            switch (configType)
            {
                case AETConfigType.ModelDryRun:
                    return DryRunAnonymisedImageFolder;
                case AETConfigType.ModelWithResultDryRun:
                    return DryRunModelWithResultFolder;
                default:
                    throw new ArgumentException("Unknown configuration type", nameof(configType));
            }
        }
    }
}