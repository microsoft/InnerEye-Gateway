namespace Microsoft.InnerEye.Listener.Tests.Models
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Threading.Tasks;

    using Dicom;

    using Microsoft.InnerEye.Azure.Segmentation.API.Common;
    using Microsoft.InnerEye.Azure.Segmentation.Client;

    public class MockInnerEyeSegmentationClient : IInnerEyeSegmentationClient
    {
        private readonly IInnerEyeSegmentationClient _InnerEyeSegmentationClient;
        private bool disposedValue;

        public MockInnerEyeSegmentationClient(IInnerEyeSegmentationClient InnerEyeSegmentationClient)
        {
            _InnerEyeSegmentationClient = InnerEyeSegmentationClient;
        }

        public string SegmentationResultFile { get; set; } = @"Images\LargeSeriesWithContour\rtstruct.dcm";

        public TimeSpan ResponseDelay { get; set; } = TimeSpan.FromMilliseconds(100);

        public ModelResult SegmentationProgressResult { get; set; }

        public Exception PingException { get; set; }

        public Exception SegmentationResultException { get; set; }

        public bool RealSegmentation { get; set; }

        public IEnumerable<DicomTagAnonymisation> SegmentationAnonymisationProtocol => _InnerEyeSegmentationClient.SegmentationAnonymisationProtocol;

        public Guid SegmentationAnonymisationProtocolId => _InnerEyeSegmentationClient.SegmentationAnonymisationProtocolId;

        /// <inheritdoc/>
        public IEnumerable<DicomTag> TopLevelReplacements =>
            _InnerEyeSegmentationClient.TopLevelReplacements;

        public Task PingAsync()
        {
            return DelayAndThrowExceptionIfNotNull(PingException);
        }

        public async Task<ModelResult> SegmentationResultAsync(string modelId, string segmentationId, IEnumerable<DicomFile> referenceDicomFiles, IEnumerable<TagReplacement> userReplacements)
        {
            await DelayAndThrowExceptionIfNotNull(SegmentationResultException).ConfigureAwait(false);

            if (RealSegmentation)
            {
                return await _InnerEyeSegmentationClient.SegmentationResultAsync(modelId, segmentationId, referenceDicomFiles, userReplacements).ConfigureAwait(false);
            }
            else if (SegmentationProgressResult != null)
            {
                return SegmentationProgressResult;
            }
            else
            {
                var dicomFile = await DicomFile.OpenAsync(SegmentationResultFile, FileReadOption.ReadAll).ConfigureAwait(false);

                dicomFile.Dataset.AddOrUpdate(DicomTag.SoftwareVersions,
                    $@"InnerEye AI Model: Test.Name\" +
                    $@"InnerEye AI Model ID: Test.ID\" +
                    $@"InnerEye Model Created: Test.CreatedDate\" +
                    $@"InnerEye Version: Test.AssemblyVersion\");

                dicomFile.Dataset.AddOrUpdate(DicomTag.SeriesDate,
                    $"{DateTime.UtcNow.Year}{DateTime.UtcNow.Month:D2}{DateTime.UtcNow.Day:D2}");

                var anonymized = DeanonymizeDicomFile(
                    dicomFile,
                    referenceDicomFiles,
                    TopLevelReplacements,
                    userReplacements,
                    SegmentationAnonymisationProtocolId,
                    SegmentationAnonymisationProtocol);
                return new ModelResult(100, string.Empty, anonymized);
            }
        }

        /// <inheritdoc/>
        public DicomFile DeanonymizeDicomFile(
            DicomFile dicomFile,
            IEnumerable<DicomFile> referenceDicomFiles,
            IEnumerable<DicomTag> topLevelReplacements,
            IEnumerable<TagReplacement> userReplacements,
            Guid anonymisationProtocolId,
            IEnumerable<DicomTagAnonymisation> anonymisationProtocol) =>
                _InnerEyeSegmentationClient.DeanonymizeDicomFile(
                    dicomFile,
                    referenceDicomFiles,
                    topLevelReplacements,
                    userReplacements,
                    anonymisationProtocolId,
                    anonymisationProtocol);

        public async Task<(string segmentationId, IEnumerable<DicomFile> postedImages)> StartSegmentationAsync(string modelId, IEnumerable<ChannelData> channelIdsAndDicomFiles)
        {
            if (RealSegmentation)
            {
                return await _InnerEyeSegmentationClient.StartSegmentationAsync(modelId, channelIdsAndDicomFiles).ConfigureAwait(false);
            }
            else
            {
                return (Guid.NewGuid().ToString(), channelIdsAndDicomFiles.SelectMany(x => x.DicomFiles).ToList());
            }
        }

        public DicomFile AnonymizeDicomFile(DicomFile dicomFile, Guid anonymisationProtocolId, IEnumerable<DicomTagAnonymisation> anonymisationProtocol)
        {
            return _InnerEyeSegmentationClient.AnonymizeDicomFile(dicomFile, anonymisationProtocolId, anonymisationProtocol);
        }

        /// <inheritdoc />
        public IEnumerable<DicomFile> AnonymizeDicomFiles(
            IEnumerable<DicomFile> dicomFiles,
            Guid anonymisationProtocolId,
            IEnumerable<DicomTagAnonymisation> anonymisationProtocol) =>
                _InnerEyeSegmentationClient.AnonymizeDicomFiles(dicomFiles, anonymisationProtocolId, anonymisationProtocol);

        private async Task DelayAndThrowExceptionIfNotNull(Exception e)
        {
            await Task.Delay(ResponseDelay).ConfigureAwait(false);

            if (e != null)
            {
                throw e;
            }
        }

        protected virtual void Dispose(bool disposing)
        {
            if (disposedValue)
            {
                return;
            }

            if (disposing)
            {
                _InnerEyeSegmentationClient?.Dispose();
            }

            disposedValue = true;
        }

        public void Dispose()
        {
            // Do not change this code. Put cleanup code in 'Dispose(bool disposing)' method
            Dispose(disposing: true);
            GC.SuppressFinalize(this);
        }
    }
}