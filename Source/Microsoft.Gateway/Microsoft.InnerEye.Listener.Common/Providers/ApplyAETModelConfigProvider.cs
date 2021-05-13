namespace Microsoft.InnerEye.Listener.Common.Providers
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using Dicom;
    using Microsoft.InnerEye.Azure.Segmentation.API.Common;
    using Microsoft.InnerEye.Azure.Segmentation.Client;
    using Microsoft.InnerEye.DicomConstraints;

    /// <summary>
    /// Helper function for applying AET Model configs.
    /// </summary>
    public static class ApplyAETModelConfigProvider
    {
        /// <summary>
        /// Returns the configuration for the given AET.
        /// </summary>
        /// <param name="clientAETList">List of AETConfigModels to search.</param>
        /// <param name="calledAET">The DICOM AET the gateway was called</param>
        /// <param name="callingAET">The DICM AET of the AE calling the gateway</param>
        /// <returns>Matching ClientAETConfig if one found, throws an exception otherwise.</returns>
        public static ClientAETConfig GetAETConfigs(IEnumerable<AETConfigModel> clientAETList, string calledAET, string callingAET)
        {
            var aetConfig = GetAETConfigModel(clientAETList, calledAET, callingAET);
            if (aetConfig != null)
            {
                return aetConfig.AETConfig;
            }
            throw new ConfigurationException($"Config for called AET {calledAET} and calling AET {callingAET} not found");
        }

        /// <summary>
        /// Returns the configuration for the given AET.
        /// </summary>
        /// <param name="clientAETList">List of AETConfigModels to search.</param>
        /// <param name="calledAET">The DICOM AET the gateway was called</param>
        /// <param name="callingAET">The DICM AET of the AE calling the gateway</param>
        /// <returns>Matching AETConfigModel if one found, null otherwise.</returns>
        public static AETConfigModel GetAETConfigModel(IEnumerable<AETConfigModel> clientAETList, string calledAET, string callingAET)
        {
            clientAETList = clientAETList ?? throw new ArgumentNullException(nameof(clientAETList));

            foreach (var aetConfig in clientAETList)
            {
                if (aetConfig.CalledAET.Equals(calledAET, System.StringComparison.Ordinal) && aetConfig.CallingAET.Equals(callingAET, System.StringComparison.Ordinal))
                {
                    return aetConfig;
                }
            }
            return null;
        }

        /// <summary>
        /// Given a set of DICOM files and a collection of modelConfig, select the model to run and define the DICOMFiles per channel.
        /// </summary>
        /// <remarks>
        /// This is a client side only helper function.
        /// If you pass dicom data spanning multiple studies this method will first group by study and only match a model
        /// if all channel constraints can be satisfied from series data within an individual study. Put another way, a model
        /// will only ever run on series from the same study.
        /// Note however, that there is nothing to stop multiple channels in the same model being satisifed by the same series.
        /// </remarks>
        /// <param name="modelsConfig"> The config to apply</param>
        /// <param name="associationData"> The set of dicomfiles to analyse and match against a model</param>
        /// <returns>The matched segmentation model (or null) and the constraint results.</returns>
        public static ConstraintResult<SegmentationModel> ApplyAETModelConfig(
            IEnumerable<ModelConstraintsConfig> modelsConfig,
            IEnumerable<DicomFile> associationData)
        {
            //checks and throws exception if modelsConfig value is null
            if (modelsConfig == null)
            {
                throw new ArgumentNullException(nameof(modelsConfig));
            }

            //checks and throws exception if associationData value is null
            if (associationData == null)
            {
                throw new ArgumentNullException(nameof(associationData));
            }

            var dicomConstraintResults = new List<DicomConstraintResult>();

            // Group by StudyUID and throw away files without studyInstanceUID
            var studyFilesCollection = associationData
                                           .GroupBy(dicomFile => dicomFile.Dataset.GetSingleValueOrDefault(DicomTag.StudyInstanceUID, string.Empty))
                                            .Where(g => !string.IsNullOrEmpty(g.Key));

            foreach (var studyFiles in studyFilesCollection)
            {
                // Group by SeriesInstanceUID and throw away files without seriesInstanceUID
                var seriesInStudyFiles = studyFiles
                                            .GroupBy(dicomFile => dicomFile.Dataset.GetSingleValueOrDefault(DicomTag.SeriesInstanceUID, string.Empty))
                                            .Where(g => !string.IsNullOrEmpty(g.Key));

                var constraintResult = FindMatchingModelAndChannels(modelsConfig, seriesInStudyFiles);

                dicomConstraintResults.AddRange(constraintResult.DicomConstraintResults);

                // As soon as we match we return the result
                if (constraintResult.Matched)
                {
                    return new ConstraintResult<SegmentationModel>(dicomConstraintResults, constraintResult.Result);
                }
            }

            return new ConstraintResult<SegmentationModel>(dicomConstraintResults);
        }

        /// <summary>
        /// Searches available models and matches models where the given seriesData satisfies the model's constraints.
        /// </summary>
        /// <param name="modelsConfig">Model configurations and their constrains</param>
        /// <param name="seriesData">List of series from the same study to try and match against a set of models</param>
        /// <returns>The first model where the given series match the models channel constraints or null if there is not match</returns>
        /// <remarks>In general we should log the reasons why a series fails a constraint - and track this in the cloud.</remarks>
        /// <exception cref="InvalidOperationException">If config modelsConfig has some invalid matching. E.g. matching string on a sequence</exception>
        private static ConstraintResult<SegmentationModel> FindMatchingModelAndChannels(
            IEnumerable<ModelConstraintsConfig> modelsConfig,
            IEnumerable<IEnumerable<DicomFile>> seriesData)
        {
            var dicomConstraintResults = new List<DicomConstraintResult>();

            // Models are in priority order
            foreach (var modelConstraint in modelsConfig)
            {
                var constraintResult = GetChannelConstraintResult(modelConstraint.ChannelConstraints, seriesData);

                dicomConstraintResults.AddRange(constraintResult.DicomConstraintResults);

                // As soon as we match we return the result
                if (constraintResult.Matched)
                {
                    return new ConstraintResult<SegmentationModel>(
                        dicomConstraintResults,
                        new SegmentationModel(modelConstraint.ModelId, constraintResult.Result, modelConstraint.TagReplacements));
                }
            }

            // No models matched
            return new ConstraintResult<SegmentationModel>(dicomConstraintResults);
        }

        /// <summary>
        /// Gets the channel constraint results for the collection of model channel constraints.
        /// </summary>
        /// <param name="channelConstraints">The channel constraints.</param>
        /// <param name="seriesData">The series data.</param>
        /// <returns>The channel data and the constraint result.</returns>
        private static ConstraintResult<IEnumerable<ChannelData>> GetChannelConstraintResult(
            IEnumerable<ModelChannelConstraints> channelConstraints,
            IEnumerable<IEnumerable<DicomFile>> seriesData)
        {
            var matched = true;

            var channelData = new List<ChannelData>();
            var dicomConstraintResults = new List<DicomConstraintResult>();

            foreach (var channelConstraint in channelConstraints)
            {
                var constraintResult = GetChannelConstraintResult(channelConstraint, seriesData);

                if (!constraintResult.Matched)
                {
                    matched = false;
                }

                channelData.Add(constraintResult.Result);
                dicomConstraintResults.AddRange(constraintResult.DicomConstraintResults);
            }

            return matched ?
                new ConstraintResult<IEnumerable<ChannelData>>(dicomConstraintResults, channelData) :
                new ConstraintResult<IEnumerable<ChannelData>>(dicomConstraintResults);
        }

        /// <summary>
        /// Attempts to find the first series that matches the constraints or returns a collection of DICOM constraint results.
        /// </summary>
        /// <param name="modelChannelConstraints">The channel constraints for the model.</param>
        /// <param name="seriesData">The collection of Dicom files for the series.</param>
        /// <returns>If matched and the channel data or false and null and the dicom constraint result per Dicom series.</returns>
        private static ConstraintResult<ChannelData> GetChannelConstraintResult(
            ModelChannelConstraints modelChannelConstraints,
            IEnumerable<IEnumerable<DicomFile>> seriesData)
        {
            var dicomConstraintResults = new List<DicomConstraintResult>();

            foreach (var dicomSeries in seriesData)
            {
                var constraintResult = GetChannelConstraintResult(modelChannelConstraints, dicomSeries);

                dicomConstraintResults.AddRange(constraintResult.DicomConstraintResults);

                // As soon as we match we return the result
                if (constraintResult.Matched)
                {
                    return new ConstraintResult<ChannelData>(dicomConstraintResults, constraintResult.Result);
                }
            }

            return new ConstraintResult<ChannelData>(dicomConstraintResults);
        }

        /// <summary>
        /// Gets the channel constraint result for a collection of DICOM files.
        /// </summary>
        /// <param name="channelConstraints">The channel constraints.</param>
        /// <param name="dicomFiles">The dicom files.</param>
        /// <returns>The channel data and constraint result.</returns>
        private static ConstraintResult<ChannelData> GetChannelConstraintResult(
                    ModelChannelConstraints channelConstraints,
                    IEnumerable<DicomFile> dicomFiles)
        {
            var (filteredDicomFiles, dicomConstraintResults) = FilterDicomFiles(channelConstraints, dicomFiles);
            var filteredDicomFilesCount = filteredDicomFiles.Count;

            // Check that we have sufficient images after the filter
            var filteredDicomFilesCountSufficient =
                channelConstraints.MinChannelImages <= filteredDicomFilesCount &&
                (channelConstraints.MaxChannelImages <= 0 || filteredDicomFilesCount <= channelConstraints.MaxChannelImages);

            if (filteredDicomFilesCountSufficient)
            {
                // we may wish to do something more sophisticated here. e.g. record all the matches and choose
                // and distribute matched series over the constraints using a heuristic
                return new ConstraintResult<ChannelData>(
                    dicomConstraintResults,
                    new ChannelData(channelID: channelConstraints.ChannelID, dicomFiles: filteredDicomFiles));
            }

            return new ConstraintResult<ChannelData>(dicomConstraintResults);
        }

        /// <summary>
        /// Filters the DICOM files based on the channel constraints..
        /// </summary>
        /// <param name="channelConstraints">The channel constraints.</param>
        /// <param name="dicomFiles">The dicom files.</param>
        /// <returns>The filtered DICOM files and the constraint results per DICOM file.</returns>
        private static (IList<DicomFile> FilteredDicomFiles, IEnumerable<DicomConstraintResult> DicomConstraintResults) FilterDicomFiles(
                    ModelChannelConstraints channelConstraints,
                    IEnumerable<DicomFile> dicomFiles)
        {
            var filteredImages = new List<DicomFile>();
            var dicomConstraintResults = new List<DicomConstraintResult>();

            foreach (var dicomFile in dicomFiles)
            {
                var imageFilterResult = channelConstraints.ImageFilter.Check(dicomFile.Dataset);
                var channelConstraintResult = channelConstraints.ChannelConstraints.Check(dicomFile.Dataset);

                dicomConstraintResults.Add(imageFilterResult);
                dicomConstraintResults.Add(channelConstraintResult);

                // This check will throw InvalidOperationException if the config is incorrect
                if (imageFilterResult.Result && channelConstraintResult.Result)
                {
                    filteredImages.Add(dicomFile);
                }
            }

            return (filteredImages, dicomConstraintResults);
        }
    }
}
