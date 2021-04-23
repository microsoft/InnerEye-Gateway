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
        /// <summary>
        /// Top  lvl replacements for deanonymizer
        /// </summary>
        private readonly IEnumerable<DicomTag> _deAnonymizeTryAddReplaceAtTopLevel = new[]
        {
            // Patient module
            DicomTag.PatientID,
            DicomTag.PatientName,
            DicomTag.PatientBirthDate,
            DicomTag.PatientSex,

            // Study module
            DicomTag.StudyDate,
            DicomTag.StudyTime,
            DicomTag.ReferringPhysicianName,
            DicomTag.StudyID,
            DicomTag.AccessionNumber,
            DicomTag.StudyDescription,
        };

        private readonly InnerEyeSegmentationClient _InnerEyeSegmentationClient;
        private bool disposedValue;

        public MockInnerEyeSegmentationClient(InnerEyeSegmentationClient InnerEyeSegmentationClient)
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

        public Task PingAsync()
        {
            return DelayAndThrowExceptionIfNotNull(PingException);
        }

        public async Task<ModelResult> SegmentationResultAsync(string modelId, string segmentationId, IEnumerable<DicomFile> referenceDicomFiles, IEnumerable<TagReplacement> userSettingsForResultRTFile)
        {
            await DelayAndThrowExceptionIfNotNull(SegmentationResultException);

            if (RealSegmentation)
            {
                return await _InnerEyeSegmentationClient.SegmentationResultAsync(modelId, segmentationId, referenceDicomFiles, userSettingsForResultRTFile);
            }
            else if (SegmentationProgressResult != null)
            {
                return SegmentationProgressResult;
            }
            else
            {
                var dicomFile = await DicomFile.OpenAsync(SegmentationResultFile, FileReadOption.ReadAll);

                dicomFile.Dataset.AddOrUpdate(DicomTag.SoftwareVersions,
                    $@"InnerEye AI Model: Test.Name\" +
                    $@"InnerEye AI Model ID: Test.ID\" +
                    $@"InnerEye Model Created: Test.CreatedDate\" +
                    $@"InnerEye Version: Test.AssemblyVersion\");

                dicomFile.Dataset.AddOrUpdate(DicomTag.SeriesDate,
                    $"{DateTime.UtcNow.Year}{DateTime.UtcNow.Month.ToString("D2")}{DateTime.UtcNow.Day.ToString("D2")}");

                var anonymized = _InnerEyeSegmentationClient.DeAnonymize(
                    dicomFile,
                    referenceDicomFiles,
                    _deAnonymizeTryAddReplaceAtTopLevel,
                    userSettingsForResultRTFile,
                    SegmentationAnonymisationProtocolId,
                    SegmentationAnonymisationProtocol);
                return new ModelResult(100, string.Empty, anonymized);
            }
        }

        public async Task<(string segmentationId, IEnumerable<DicomFile> postedImages)> StartSegmentationAsync(string modelId, IEnumerable<ChannelData> dicomFiles)
        {
            if (RealSegmentation)
            {
                return await _InnerEyeSegmentationClient.StartSegmentationAsync(modelId, dicomFiles);
            }
            else
            {
                return (Guid.NewGuid().ToString(), dicomFiles.SelectMany(x => x.DicomFiles).ToList());
            }
        }

        public DicomFile AnonymizeDicomFile(DicomFile dicomFile, Guid anonymisationProtocolId, IEnumerable<DicomTagAnonymisation> anonymisationProtocol)
        {
            return _InnerEyeSegmentationClient.AnonymizeDicomFile(dicomFile, anonymisationProtocolId, anonymisationProtocol);
        }

        private async Task DelayAndThrowExceptionIfNotNull(Exception e)
        {
            await Task.Delay(ResponseDelay);

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