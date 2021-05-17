// Copyright (c) Microsoft Corporation.
// Licensed under the MIT license.

namespace Microsoft.InnerEye.Listener.DataProvider.Implementations
{
    using System;
    using System.Collections.Generic;
    using System.Diagnostics;
    using System.Globalization;
    using System.Linq;
    using System.Net.Sockets;
    using System.Threading.Tasks;
    using Dicom.Network;
    using Interfaces;
    using Models;
    using DicomClient = Dicom.Network.Client.DicomClient;

    /// <summary>
    /// A wrapper for sending data over Dicom.
    /// </summary>
    public class DicomDataSender : IDicomDataSender
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="DicomDataSender"/> class.
        /// </summary>
        public DicomDataSender()
        {
        }

        /// <inheritdoc />
        /// <exception cref="ArgumentNullException">If the peerApplicationEntity is null.</exception>
        /// <exception cref="ArgumentException">
        /// peerApplicationEntity
        /// or
        /// ownApplicationEntityTitle
        /// </exception>
        public async Task<DicomOperationResult> DicomEchoAsync(
            string ownApplicationEntityTitle,
            string peerApplicationEntityTitle,
            int peerApplicationEntityPort,
            string peerApplicationEntityIPAddress)
        {
            // Validate the inputs
            ValidateDicomApplicationEntitySettings(
                ownApplicationEntityTitle,
                peerApplicationEntityTitle,
                peerApplicationEntityPort,
                peerApplicationEntityIPAddress);

            var result = DicomOperationResult.Error;
            var dicomClient = CreateDicomClient(
                    host: peerApplicationEntityIPAddress,
                    port: peerApplicationEntityPort,
                    useTls: false,
                    callingAe: ownApplicationEntityTitle,
                    calledAe: peerApplicationEntityTitle);

            await dicomClient.AddRequestAsync(new DicomCEchoRequest
            {
                OnResponseReceived = (request, response) =>
                {
                    // The Dicom Client send method waits until the response received has finished.
                    // Do not put any async code in here as it will not wait.
                    result = GetStatus(response.Status);
                }
            }).ConfigureAwait(false);

            try
            {
                await dicomClient.SendAsync().ConfigureAwait(false);
            }
            catch (SocketException e)
            {
                result = DicomOperationResult.NoResponse;

                Trace.TraceError($"[{GetType().Name}] A socket exception occured during the Dicom echo. Failed to get a response. Exception: {e}");
            }
#pragma warning disable CA1031 // Do not catch general exception types
            catch (Exception e)
#pragma warning restore CA1031 // Do not catch general exception types
            {
                Trace.TraceError($"[{GetType().Name}] An unkown exception occured during the Dicom echo. Exception: {e}");
            }

            return result;
        }

        /// <inheritdoc />
        /// <exception cref="ArgumentException">
        /// peerApplicationEntity
        /// or
        /// ownApplicationEntityTitle
        /// </exception>
        /// <exception cref="ArgumentNullException">If the files are null.</exception>
        /// <exception cref="SocketException">If the C-Store request can not communicate with the peer application entity.</exception>
        public async Task<IEnumerable<Tuple<Dicom.DicomFile, DicomOperationResult>>> SendFilesAsync(
            string ownApplicationEntityTitle,
            string peerApplicationEntityTitle,
            int peerApplicationEntityPort,
            string peerApplicationEntityIPAddress,
            params Dicom.DicomFile[] dicomFiles)
        {
            // Validate the inputs
            ValidateDicomApplicationEntitySettings(
                ownApplicationEntityTitle,
                peerApplicationEntityTitle,
                peerApplicationEntityPort,
                peerApplicationEntityIPAddress);

            var result = new List<Tuple<Dicom.DicomFile, DicomOperationResult>>();

            var dicomClient = CreateDicomClient(
                    host: peerApplicationEntityIPAddress,
                    port: peerApplicationEntityPort,
                    useTls: false,
                    callingAe: ownApplicationEntityTitle,
                    calledAe: peerApplicationEntityTitle);

            var filesToSend = 0;

            foreach (var dicomFile in dicomFiles)
            {
                try
                {
                    // Constructor will throw if the Dicom file is invalid
                    var dicomStoreRequest = new DicomCStoreRequest(dicomFile)
                    {
                        // The Dicom Client send method waits until the response received has finished.
                        // Do not put any async code in here as it will not wait.
                        OnResponseReceived = (request, response) =>
                        {
                            Trace.TraceInformation($"[DicomCStoreRequest - OnResponseReceived] Dicom Dataset: {dicomFile}     Status: {response.Status}");
                            result.Add(Tuple.Create(dicomFile, GetStatus(response.Status)));
                        },
                    };

                    await dicomClient.AddRequestAsync(dicomStoreRequest).ConfigureAwait(false);
                    filesToSend++;
                }
#pragma warning disable CA1031 // Do not catch general exception types
                catch (Exception e)
#pragma warning restore CA1031 // Do not catch general exception types
                {
                    Trace.TraceWarning($"[DicomCStoreRequest] Could not send Dicom Dataset: {dicomFile}. Exception: {e}");
                }
            }

            await dicomClient.SendAsync().ConfigureAwait(false);

            // Add any missing results
            foreach (var dicomFile in dicomFiles)
            {
                if (result.FirstOrDefault(x => x.Item1 == dicomFile) == null)
                {
                    result.Add(Tuple.Create(dicomFile, DicomOperationResult.Error));
                }
            }

            return result;
        }

        /// <summary>
        /// Validates the dicom application entity settings.
        /// </summary>
        /// <param name="ownApplicationEntityTitle">The own application entity title.</param>
        /// <param name="peerApplicationEntityTitle">The peer application entity title.</param>
        /// <param name="peerApplicationEntityPort">The peer application entity port.</param>
        /// <param name="peerApplicationEntityIPAddress">The peer application entity IP address.</param>
        /// <exception cref="ArgumentNullException">peerApplicationEntity</exception>
        /// <exception cref="ArgumentException">
        /// peerApplicationEntity
        /// or
        /// ownApplicationEntityTitle
        /// </exception>
        protected static void ValidateDicomApplicationEntitySettings(
            string ownApplicationEntityTitle,
            string peerApplicationEntityTitle,
            int peerApplicationEntityPort,
            string peerApplicationEntityIPAddress)
        {
            var peerApplicationEntityTitleValidationResult = ApplicationEntityValidationHelpers.ValidateTitle(peerApplicationEntityTitle);
            var peerApplicationEntityPortValidationResult = ApplicationEntityValidationHelpers.ValidatePort(peerApplicationEntityPort);
            var peerApplicationEntityIPAddressValidationResult = ApplicationEntityValidationHelpers.ValidateIPAddress(peerApplicationEntityIPAddress);

            var result = peerApplicationEntityTitleValidationResult && peerApplicationEntityPortValidationResult && peerApplicationEntityIPAddressValidationResult;

            Trace.TraceInformation(
                string.Format(CultureInfo.InvariantCulture, "[DicomDataSender] Validation result {0}.", result),
                new Dictionary<string, object>() {
                    { "PeerApplicationEntityTitle", peerApplicationEntityTitle },
                    { "PeerApplicationEntityPort", peerApplicationEntityPort },
                    { "PeerApplicationEntityIPAddress", peerApplicationEntityIPAddress },
                    { "PeerApplicationEntityTitleValidationResult", peerApplicationEntityTitleValidationResult },
                    { "PeerApplicationEntityPortValidationResult", peerApplicationEntityPortValidationResult },
                    { "PeerApplicationEntityIpAddressValidationResult", peerApplicationEntityIPAddressValidationResult }
                });

            if (!result)
            {
                throw new ArgumentException("The peer application entity is invalid.", nameof(peerApplicationEntityTitle));
            }

            if (!ApplicationEntityValidationHelpers.ValidateTitle(ownApplicationEntityTitle))
            {
                throw new ArgumentException("The application entity title is invalid.", nameof(ownApplicationEntityTitle));
            }
        }

        /// <summary>
        /// Converts a dicom status to a Dicom operation result.
        /// </summary>
        /// <param name="status">The Dicom status.</param>
        /// <returns>The Dicom operation result.</returns>
        private static DicomOperationResult GetStatus(DicomStatus status)
        {
            // On any warning we still return success. Please check here for a list of warnings: https://fo-dicom.github.io/html/f270d490-66d6-28d5-1fa3-f619b4792034.htm
            return status == DicomStatus.Success ||
                    status == DicomStatus.StorageCoercionOfDataElements ||
                    status == DicomStatus.StorageElementsDiscarded
                ? DicomOperationResult.Success : DicomOperationResult.Error;
        }

        /// <summary>
        /// Gets a Dicom client.
        /// </summary>
        /// <returns>The Dicom client.</returns>
        private static DicomClient CreateDicomClient(string host, int port, bool useTls, string callingAe, string calledAe)
        {
            return new DicomClient(host, port, useTls, callingAe, calledAe);
        }
    }
}