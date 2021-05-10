namespace Microsoft.InnerEye.Azure.Segmentation.Client
{
    using System;
    using System.Collections.Generic;
    using System.Diagnostics;
    using System.IO;
    using System.IO.Compression;
    using System.Linq;
    using System.Net;
    using System.Net.Http;
    using System.Security.Authentication;
    using System.Threading.Tasks;

    using Dicom;

    using DICOMAnonymizer;
    using Microsoft.InnerEye.Azure.Segmentation.API.Common;
    using static DICOMAnonymizer.AnonymizeEngine;

    /// <summary>
    /// The InnerEye segmentation client.
    /// </summary>
    /// <seealso cref="IInnerEyeSegmentationClient" />
    public sealed class InnerEyeSegmentationClient : IInnerEyeSegmentationClient
    {
        private const string AuthTokenHeaderName = "API_AUTH_SECRET";

        /// <summary>
        /// The anonymisation protocol used for any segmentation. If you modify this protocol please update the protocol identifer.
        /// </summary>
        private static readonly IEnumerable<DicomTagAnonymisation> _segmentationAnonymisationProtocol = new[]
        {
            // Geometry
            new DicomTagAnonymisation(DicomTag.PatientPosition, AnonymisationMethod.Keep),
            new DicomTagAnonymisation(DicomTag.Columns, AnonymisationMethod.Keep),
            new DicomTagAnonymisation(DicomTag.Rows, AnonymisationMethod.Keep),
            new DicomTagAnonymisation(DicomTag.PixelSpacing, AnonymisationMethod.Keep),
            new DicomTagAnonymisation(DicomTag.ImagePositionPatient, AnonymisationMethod.Keep),
            new DicomTagAnonymisation(DicomTag.ImageOrientationPatient, AnonymisationMethod.Keep),
            new DicomTagAnonymisation(DicomTag.SliceLocation, AnonymisationMethod.Keep),
            new DicomTagAnonymisation(DicomTag.BodyPartExamined, AnonymisationMethod.Keep),

            // Modality
            new DicomTagAnonymisation(DicomTag.Modality, AnonymisationMethod.Keep),
            new DicomTagAnonymisation(DicomTag.ModalityLUTSequence, AnonymisationMethod.Keep),
            new DicomTagAnonymisation(DicomTag.HighBit, AnonymisationMethod.Keep),
            new DicomTagAnonymisation(DicomTag.BitsStored, AnonymisationMethod.Keep),
            new DicomTagAnonymisation(DicomTag.BitsAllocated, AnonymisationMethod.Keep),
            new DicomTagAnonymisation(DicomTag.SamplesPerPixel, AnonymisationMethod.Keep),
            new DicomTagAnonymisation(DicomTag.PixelData, AnonymisationMethod.Keep),
            new DicomTagAnonymisation(DicomTag.PhotometricInterpretation, AnonymisationMethod.Keep),
            new DicomTagAnonymisation(DicomTag.PixelRepresentation, AnonymisationMethod.Keep),
            new DicomTagAnonymisation(DicomTag.RescaleIntercept, AnonymisationMethod.Keep),
            new DicomTagAnonymisation(DicomTag.RescaleSlope, AnonymisationMethod.Keep),
            new DicomTagAnonymisation(DicomTag.ImageType, AnonymisationMethod.Keep),

            // UIDs
            new DicomTagAnonymisation(DicomTag.PatientID, AnonymisationMethod.Hash),
            new DicomTagAnonymisation(DicomTag.SeriesInstanceUID, AnonymisationMethod.Hash),
            new DicomTagAnonymisation(DicomTag.StudyInstanceUID, AnonymisationMethod.Hash),
            new DicomTagAnonymisation(DicomTag.SOPInstanceUID, AnonymisationMethod.Hash),
            new DicomTagAnonymisation(DicomTag.SOPClassUID, AnonymisationMethod.Keep),

            // RT
            // RT DicomFrameOfReference
            new DicomTagAnonymisation(DicomTag.FrameOfReferenceUID, AnonymisationMethod.Hash),
            new DicomTagAnonymisation(DicomTag.RTReferencedStudySequence, AnonymisationMethod.Keep),

            // RT DicomRTContour
            new DicomTagAnonymisation(DicomTag.ReferencedROINumber, AnonymisationMethod.Keep),
            new DicomTagAnonymisation(DicomTag.ROIDisplayColor, AnonymisationMethod.Keep),
            new DicomTagAnonymisation(DicomTag.ContourSequence, AnonymisationMethod.Keep),
            new DicomTagAnonymisation(DicomTag.ROIContourSequence, AnonymisationMethod.Keep),

            // RT DicomRTContourImageItem
            new DicomTagAnonymisation(DicomTag.ReferencedSOPClassUID, AnonymisationMethod.Keep),
            new DicomTagAnonymisation(DicomTag.ReferencedSOPInstanceUID, AnonymisationMethod.Hash),

            // RT DicomRTContourItem
            new DicomTagAnonymisation(DicomTag.NumberOfContourPoints, AnonymisationMethod.Keep),
            new DicomTagAnonymisation(DicomTag.ContourData, AnonymisationMethod.Keep),
            new DicomTagAnonymisation(DicomTag.ContourGeometricType, AnonymisationMethod.Keep),
            new DicomTagAnonymisation(DicomTag.ContourImageSequence, AnonymisationMethod.Keep),

            // RT DicomRTObservation
            new DicomTagAnonymisation(DicomTag.RTROIObservationsSequence, AnonymisationMethod.Keep),
            new DicomTagAnonymisation(DicomTag.ObservationNumber, AnonymisationMethod.Keep),

            // RT DicomRTReferencedStudy
            new DicomTagAnonymisation(DicomTag.RTReferencedSeriesSequence, AnonymisationMethod.Keep),

            // RT DicomRTStructureSet
            new DicomTagAnonymisation(DicomTag.StructureSetLabel, AnonymisationMethod.Hash),
            new DicomTagAnonymisation(DicomTag.StructureSetName, AnonymisationMethod.Hash),
            new DicomTagAnonymisation(DicomTag.ReferencedFrameOfReferenceSequence, AnonymisationMethod.Keep),

                // RT DicomRTStructureSetROI
            new DicomTagAnonymisation(DicomTag.ROINumber, AnonymisationMethod.Keep),
            new DicomTagAnonymisation(DicomTag.ROIName, AnonymisationMethod.Keep),
            new DicomTagAnonymisation(DicomTag.ReferencedFrameOfReferenceUID, AnonymisationMethod.Hash),
            new DicomTagAnonymisation(DicomTag.ROIGenerationAlgorithm, AnonymisationMethod.Keep),
            new DicomTagAnonymisation(DicomTag.StructureSetROISequence, AnonymisationMethod.Keep),
        };

        private readonly HttpClientHandler _httpClientHandler;
        private readonly RetryHandler _retryHandler;
        private readonly HttpClient _client;

        /// <summary>
        /// The de anonymize try add replace at top level
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

        /// <inheritdoc/>
        public IEnumerable<DicomTag> TopLevelReplacements =>
            _deAnonymizeTryAddReplaceAtTopLevel;

        /// <summary>
        /// Initializes a new instance of the <see cref="InnerEyeSegmentationClient"/> class.
        /// </summary>
        /// <param name="baseAddress">The base address.</param>
        /// <param name="licenseKey">The license key.</param>
        public InnerEyeSegmentationClient(
            Uri baseAddress,
            string licenseKey)
        {
            HttpClientHandler httpHandler = null;
            RetryHandler retryHandler = null;
            HttpClient client = null;

            try
            {
                var timeOut = TimeSpan.FromMinutes(10);
                httpHandler = new HttpClientHandler();
                retryHandler = new RetryHandler(httpHandler);
#pragma warning disable IDE0017 // Simplify object initialization
                client = new HttpClient(retryHandler);
#pragma warning restore IDE0017 // Simplify object initialization

                client.BaseAddress = baseAddress;
                client.Timeout = timeOut;
                client.DefaultRequestHeaders.Add(AuthTokenHeaderName, licenseKey);

                _httpClientHandler = httpHandler;
                _retryHandler = retryHandler;
                _client = client;

                httpHandler = null;
                retryHandler = null;
                client = null;
            }
            finally
            {
#pragma warning disable CA1508 // Avoid dead conditional code
                httpHandler?.Dispose();
                retryHandler?.Dispose();
                client?.Dispose();
#pragma warning restore CA1508 // Avoid dead conditional code
            }
        }

        /// <inheritdoc />
        public IEnumerable<DicomTagAnonymisation> SegmentationAnonymisationProtocol => _segmentationAnonymisationProtocol;

        /// <inheritdoc />
        public Guid SegmentationAnonymisationProtocolId => new Guid("f336816b-4de8-4633-9056-fbe0fe007a03");

        /// <inheritdoc />
        public DicomFile AnonymizeDicomFile(DicomFile dicomFile, Guid anonymisationProtocolId, IEnumerable<DicomTagAnonymisation> anonymisationProtocol)
        {
            return AnonymizeDicomFile(dicomFile, GetAnonymisationEngine(anonymisationProtocolId, anonymisationProtocol));
        }

        /// <inheritdoc />
        public IEnumerable<DicomFile> AnonymizeDicomFiles(IEnumerable<DicomFile> dicomFiles, Guid anonymisationProtocolId, IEnumerable<DicomTagAnonymisation> anonymisationProtocol)
        {
            if (dicomFiles == null)
            {
                throw new ArgumentNullException(nameof(dicomFiles), "The dicom files are null.");
            }

            var anonymisationEngine = GetAnonymisationEngine(anonymisationProtocolId, anonymisationProtocol);

            // Anonymize
            return dicomFiles.Select(file => AnonymizeDicomFile(file, anonymisationEngine));
        }

        /// <inheritdoc />
        public async Task<ModelResult> SegmentationResultAsync(
            string modelId,
            string segmentationId,
            IEnumerable<DicomFile> referenceDicomFiles,
            IEnumerable<TagReplacement> userReplacements)
        {
            var modelResult = await SegmentationResultAsync(modelId, segmentationId);
            if (modelResult.DicomResult != null)
            {
                var anonymizedDicomFile = DeanonymizeDicomFile(
                    modelResult.DicomResult,
                    referenceDicomFiles,
                    TopLevelReplacements,
                    userReplacements,
                    SegmentationAnonymisationProtocolId,
                    SegmentationAnonymisationProtocol);
                return new ModelResult(modelResult.Progress, modelResult.Error, anonymizedDicomFile);
            }

            return modelResult;
        }


        /// <summary>
        /// Gets the RT file for a segmentation
        /// </summary>
        /// <param name="modelId">The model identifier.</param>
        /// <param name="segmentationId">The segmentation identifier.</param>
        /// <returns></returns>
        private async Task<ModelResult> SegmentationResultAsync(
           string modelId,
           string segmentationId)
        {
            var response = await _client.GetAsync(new Uri($@"/v1/model/results/{segmentationId}", UriKind.Relative));

            if (response.StatusCode == HttpStatusCode.Accepted)
            {
                var message = await response.Content.ReadAsStringAsync();
                return new ModelResult(50, message, null);
            }
            else if (response.StatusCode == HttpStatusCode.OK)
            {
                var zipStream = await response.Content.ReadAsByteArrayAsync();
                using (var archive = new ZipArchive(new MemoryStream(zipStream)))
                {
                    if (archive.Entries.Count != 1)
                    {
                        throw new NotSupportedException("Only 1 file is supported");
                    }

                    foreach (var entry in archive.Entries)
                    {
                        using (var stream = entry.Open())
                        {
                            var memoryStream = new MemoryStream();
                            stream.CopyTo(memoryStream);
                            memoryStream.Seek(0, SeekOrigin.Begin);
                            return new ModelResult(100, string.Empty, DicomFile.Open(memoryStream, FileReadOption.ReadAll));
                        }
                    }
                }
            }

            if (response.StatusCode == HttpStatusCode.NotFound)
            {
                throw new ArgumentException($"SegmentationId run not found {segmentationId} for model {modelId}");
            }

            throw new Exception(response.ReasonPhrase);
        }

        /// <inheritdoc />
        public async Task<(string segmentationId, IEnumerable<DicomFile> postedImages)> StartSegmentationAsync(
            string modelId,
            IEnumerable<ChannelData> channelIdsAndDicomFiles)
        {
            if (channelIdsAndDicomFiles == null)
            {
                throw new ArgumentNullException(nameof(channelIdsAndDicomFiles), "dicomFiles is null.");
            }

            if (channelIdsAndDicomFiles.Any(x => !x.DicomFiles.Any()))
            {
                throw new ArgumentException("No dicomFiles in some channelId", nameof(channelIdsAndDicomFiles));
            }

            // Anonymise data
            var anonymisedDicomData = channelIdsAndDicomFiles.Select(x => new ChannelData(x.ChannelID, AnonymizeDicomFiles(x.DicomFiles, SegmentationAnonymisationProtocolId, SegmentationAnonymisationProtocol)));

            // Compress anonymised data
            var dataZipped = DicomCompressionHelpers.CompressDicomFiles(
                channels: anonymisedDicomData,
                compressionLevel: DicomCompressionHelpers.DefaultCompressionLevel);

            using (var content = new ByteArrayContent(dataZipped))
            {
                // POST
                var response = await _client.PostAsync(new Uri($@"/v1/model/start/{modelId}", UriKind.Relative), content);

                if (response.StatusCode.Equals(HttpStatusCode.BadRequest))
                {
                    throw new ArgumentException(response.ReasonPhrase);
                }

                if (!response.IsSuccessStatusCode)
                {
                    throw new Exception(response.ReasonPhrase);
                }

                var segmentationId = await response.Content.ReadAsStringAsync();
                Trace.TraceInformation($"Segmentation uploaded with id={segmentationId}");
                var flatAnonymizedDicomFiles = anonymisedDicomData.SelectMany(x => x.DicomFiles);
                return (segmentationId, flatAnonymizedDicomFiles);
            }
        }

        /// <inheritdoc />
        public async Task PingAsync()
        {
            var response = await _client.GetAsync(new Uri("v1/ping", UriKind.Relative));

            if (response.StatusCode == HttpStatusCode.Forbidden)
            {
                throw new AuthenticationException("Invalid license key");
            }

            response.EnsureSuccessStatusCode();
        }

        /// <summary>
        /// Dispose diposable objects
        /// </summary>
        public void Dispose()
        {
            _client.Dispose();
            _retryHandler.Dispose();
            _httpClientHandler.Dispose();
        }

        /// <inheritdoc/>
        public DicomFile DeanonymizeDicomFile(
            DicomFile dicomFile,
            IEnumerable<DicomFile> referenceDicomFiles,
            IEnumerable<DicomTag> topLevelReplacements,
            IEnumerable<TagReplacement> userReplacements,
            Guid anonymisationProtocolId,
            IEnumerable<DicomTagAnonymisation> anonymisationProtocol)
        {
            // Validation
            dicomFile = dicomFile ?? throw new ArgumentNullException(nameof(dicomFile));
            referenceDicomFiles = referenceDicomFiles ?? throw new ArgumentNullException(nameof(referenceDicomFiles));
            userReplacements = userReplacements ?? throw new ArgumentNullException(nameof(userReplacements));
            topLevelReplacements = topLevelReplacements ?? throw new ArgumentNullException(nameof(topLevelReplacements));

            var firstReferenceFile = referenceDicomFiles.First().Dataset;

            foreach (var tagReplacement in topLevelReplacements)
            {
                TagReplacer.SimpleTopLevelAddOrUpdate(dicomFile.Dataset, firstReferenceFile, tagReplacement);
            }

            // Replace all hashed tags
            TagReplacer.ReplaceHashedValues(
                dicomFile: dicomFile,
                referenceDicomFiles: referenceDicomFiles.Select(x => (x, AnonymizeDicomFile(x, anonymisationProtocolId, anonymisationProtocol))),
                hashedDicomTags: anonymisationProtocol.Where(x => x.AnonymisationProtocol == AnonymisationMethod.Hash).Select(x => x.DicomTagIndex.DicomTag));

            foreach (var replacement in userReplacements)
            {
                TagReplacer.ReplaceUserTag(dicomFile.Dataset, replacement);
            }

            return dicomFile;
        }

        /// <summary>
        /// Gets the anonymisation engine for the input protocol. If the input protocol is null we will fallback
        /// to using the segmentation service anonymisation protocol.
        /// </summary>
        /// <param name="anonymisationProtocolId">The anonymisation protocol unqiue identifier.</param>
        /// <param name="anonymisationProtocol">The anonymisation protocol.</param>
        /// <returns>The anonymisation engine.</returns>
        private static AnonymizeEngine GetAnonymisationEngine(Guid anonymisationProtocolId, IEnumerable<DicomTagAnonymisation> anonymisationProtocol)
        {
            var anonymisationEngine = new AnonymizeEngine(Mode.blank);
            var tagHandler = new AnonymisationTagHandler(anonymisationProtocolId, anonymisationProtocol);

            anonymisationEngine.RegisterHandler(tagHandler);

            Trace.TraceInformation(string.Join(Environment.NewLine, anonymisationEngine.ReportRegisteredHandlers()));

            return anonymisationEngine;
        }

        /// <summary>
        /// Anonymizes a single Dicom file.
        /// </summary>
        /// <param name="dicomFile">The Dicom file to anonymize.</param>
        /// <param name="anonymisationEngine">The anonymisation engine.</param>
        /// <returns>The aonymized Dicom file.</returns>
        private static DicomFile AnonymizeDicomFile(DicomFile dicomFile, AnonymizeEngine anonymisationEngine)
        {
            if (dicomFile == null)
            {
                throw new ArgumentNullException(nameof(dicomFile));
            }

            return anonymisationEngine.Anonymize(dicomFile);
        }
    }
}