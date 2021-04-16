namespace Microsoft.InnerEye.Listener.DataProvider.Implementations
{
    using System;
    using System.Diagnostics;
    using System.Globalization;
    using System.Text;
    using System.Threading;
    using System.Threading.Tasks;
    using Dicom;
    using Dicom.Log;
    using Dicom.Network;

    using Models;

    /// <summary>
    /// The listener dicom service.
    /// </summary>
    public class ListenerDicomService : DicomService, IDicomServiceProvider, IDicomCStoreProvider, IDicomCEchoProvider
    {
        /// <summary>
        /// The current transfer ID.
        /// </summary>
        private readonly Guid _transferId;

        /// <summary>
        /// The date time of the socket connection.
        /// </summary>
        private readonly DateTime _socketConnectionDateTime;

        /// <summary>
        /// The current Dicom association.
        /// </summary>
        private DicomAssociation _currentDicomAssociation;

        /// <summary>
        /// Parameters that contain how we communicate back with the listener and save files.
        /// </summary>
        private DicomFileStoreParameters _parameters;

        /// <summary>
        /// Default fo-dicom constructor.
        /// </summary>
        /// <param name="stream">The network stream.</param>
        /// <param name="fallbackEncoding">The fallback encoding.</param>
        /// <param name="log">The logger.</param>
        public ListenerDicomService(INetworkStream stream, Encoding fallbackEncoding, Logger log)
            : base(stream, fallbackEncoding, log)
        {
            _transferId = Guid.NewGuid();
            _socketConnectionDateTime = DateTime.UtcNow;
        }

        /// <summary>
        /// Called from within fo-dicom when we receive an association request.
        /// </summary>
        /// <param name="association">The association details.</param>
        public Task OnReceiveAssociationRequestAsync(DicomAssociation association)
        {
            var parameters = GetParameters();

            if (parameters == null)
            {
                Trace.TraceError(FormatLogStatement("How did this happen? The parameters are null."));

                // Permanent reject as this should not happen and we cannot recover from this
                return SendAssociationRejectAsync(DicomRejectResult.Permanent, DicomRejectSource.ServiceProviderACSE, DicomRejectReason.NoReasonGiven);
            }

            // This should not happen otherwise something has gone wrong.
            if (_currentDicomAssociation != null)
            {
                Trace.TraceError(FormatLogStatement("Our understanding of the FO-Dicom Listener Dicom Service is incorrect. We have received multiple association requests in the same object instance."));

                return SendAssociationRejectAsync(DicomRejectResult.Permanent, DicomRejectSource.ServiceProviderACSE, DicomRejectReason.TemporaryCongestion); ;
            }

            if (association == null)
            {
                Trace.TraceError(FormatLogStatement("Received association with a null Dicom association object."));

                return SendAssociationRejectAsync(DicomRejectResult.Permanent, DicomRejectSource.ServiceProviderACSE, DicomRejectReason.NoReasonGiven); ;
            }

            _currentDicomAssociation = association;

            Trace.TraceInformation(FormatLogStatement("Receive association requested."));

            var serviceProtocols = parameters.GetAcceptedTransferSyntaxes();

            // Filter by supported PC's
            foreach (var presentationContext in association.PresentationContexts)
            {
                if (serviceProtocols.ContainsKey(presentationContext.AbstractSyntax))
                {
                    if (!presentationContext.AcceptTransferSyntaxes(serviceProtocols[presentationContext.AbstractSyntax], true))
                    {
                        Trace.TraceInformation(FormatLogStatement("Presentation Context rejected: no supported transfer syntaxes."));
                    }
                }
                else
                {
                    Trace.TraceInformation(FormatLogStatement(
                        string.Format(
                            CultureInfo.InvariantCulture,
                            "Association requested unsupported SOP class: {0}",
                            presentationContext.AbstractSyntax)));

                    presentationContext.AcceptTransferSyntaxes(new DicomTransferSyntax[0], true);
                }
            }

            parameters.Update?.Invoke(_transferId, _socketConnectionDateTime, _currentDicomAssociation, DicomReceiveProgressCode.AssociationEstablished);

            return SendAssociationAcceptAsync(association);
        }

        /// <summary>
        /// Called from within fo-dicom when we want to release the association.
        /// </summary>
        public Task OnReceiveAssociationReleaseRequestAsync()
        {
            Trace.TraceInformation(FormatLogStatement("Receive association release requested."));

            _parameters.Update?.Invoke(_transferId, _socketConnectionDateTime, _currentDicomAssociation, DicomReceiveProgressCode.AssociationReleased);

            return SendAssociationReleaseResponseAsync();
        }

        /// <summary>
        /// Called from within fo-dicom when a receive is aborted.
        /// </summary>
        /// <param name="source">The abort source.</param>
        /// <param name="reason">The abort reason.</param>
        public void OnReceiveAbort(DicomAbortSource source, DicomAbortReason reason)
        {
            Trace.TraceInformation(FormatLogStatement(
                string.Format(
                    CultureInfo.InvariantCulture,
                    "{0} aborted the receive with reason {1}.",
                    source,
                    reason)));

            _parameters.Update?.Invoke(_transferId, _socketConnectionDateTime, _currentDicomAssociation, DicomReceiveProgressCode.TransferAborted);
        }

        /// <summary>
        /// Called from within fo-dicom when a connection is closed.
        /// </summary>
        /// <param name="exception">If this was a forced connection closed this will contain the exception details.</param>
        public void OnConnectionClosed(Exception exception)
        {
            Trace.TraceInformation(FormatLogStatement("Connection closed."));

            _parameters.Update?.Invoke(_transferId, _socketConnectionDateTime, _currentDicomAssociation, DicomReceiveProgressCode.ConnectionClosed);
        }

        /// <summary>
        /// Called from within fo-dicom when a C-Store is requested.
        /// </summary>
        /// <param name="request">The C-Store request details.</param>
        /// <returns>Our response depending on whether we managed to save the incoming data.</returns>
        public DicomCStoreResponse OnCStoreRequest(DicomCStoreRequest request)
        {
            if (request == null)
            {
                throw new ArgumentNullException(nameof(request), "The request should never be null, but it is.");
            }

            Trace.TraceInformation(FormatLogStatement("C-Store request."));

            // Save the file to disk and get the file path for where it is saved
            try
            {
                _parameters.DicomSaver.SaveDicom(_transferId, request, request.File);
                _parameters.Update?.Invoke(_transferId, _socketConnectionDateTime, _currentDicomAssociation, DicomReceiveProgressCode.FileReceived);

                Trace.TraceInformation(FormatLogStatement(string.Format(CultureInfo.InvariantCulture, "Received {0}.", request.SOPInstanceUID.UID)));

                return new DicomCStoreResponse(request, DicomStatus.Success);
            }

            catch (DicomStoreException storeException)
            {
                var progressCode = DicomReceiveProgressCode.ErrorSavingFile;

                if (storeException.Status == DicomStatus.StorageCannotUnderstand)
                {
                    progressCode = DicomReceiveProgressCode.ErrorCouldNotUnderstand;
                }
                if (storeException.Status == DicomStatus.StorageStorageOutOfResources)
                {
                    progressCode = DicomReceiveProgressCode.GenericStorageException;
                }

                _parameters.Update?.Invoke(_transferId, _socketConnectionDateTime, _currentDicomAssociation, progressCode);

                Trace.TraceError(storeException.Message);

                return new DicomCStoreResponse(request, storeException.Status);
            }

            catch (Exception e)
            {
                _parameters.Update?.Invoke(_transferId, _socketConnectionDateTime, _currentDicomAssociation, DicomReceiveProgressCode.ErrorSavingFile);

                Trace.TraceError(e.Message);

                return new DicomCStoreResponse(request, DicomStatus.ProcessingFailure);
            }
        }

        /// <summary>
        /// Called from within fo-dicom is the SopInstance could not be parsed. 
        /// </summary>
        /// <param name="tempFileName">The temporary file name.</param>
        /// <param name="e">The exception detail.</param>
        public void OnCStoreRequestException(string tempFileName, Exception e)
        {
            Trace.TraceError(FormatLogStatement(string.Format(CultureInfo.InvariantCulture, "C-Store exception {0}.", e?.Message)));

            // Note that fo-dicom sends an error response here and continues to listen...
            _parameters.Update?.Invoke(_transferId, _socketConnectionDateTime, _currentDicomAssociation, DicomReceiveProgressCode.TransferAborted);
        }

        /// <summary>
        /// Called from within fo-dicom when a C-Echo is requested.
        /// </summary>
        /// <param name="request">The request details.</param>
        /// <returns>A success response.</returns>
        public DicomCEchoResponse OnCEchoRequest(DicomCEchoRequest request)
        {
            Trace.TraceInformation(FormatLogStatement("C-Store echo request."));

            _parameters.Update?.Invoke(_transferId, _socketConnectionDateTime, _currentDicomAssociation, DicomReceiveProgressCode.Echo);

            return new DicomCEchoResponse(request, DicomStatus.Success);
        }

        /// <summary>
        /// Gets the parameters.
        /// FO-dicom server initialization does not appear to be thread safe so we use this code to give it ample time to init.
        /// Please access the parameters by calling GetParameters() in an association request.
        /// </summary>
        /// <param name="timeoutSeconds">The time we will wait until the parameters are not null.</param>
        /// <returns>The parameters.</returns>
        private DicomFileStoreParameters GetParameters(uint timeoutSeconds = 5)
        {
            _parameters = UserState as DicomFileStoreParameters;

            if (_parameters == null && timeoutSeconds > 0)
            {
                SpinWait.SpinUntil(() => _parameters != null, TimeSpan.FromSeconds(timeoutSeconds));
            }

            if (_parameters == null)
            {
                Trace.TraceError("No FileStoreParameters object provided at DicomServer initialization. Incoming DICOM requests will not be handled correctly.");
            }
            else if (_parameters?.DicomSaver == null)
            {
                Trace.TraceWarning("File Store service is created, but no image saver class provided. Incoming images will not be saved.");
            }

            return _parameters;
        }

        /// <summary>
        /// Gets for formatted statement for logging.
        /// </summary>
        /// <param name="value">The inner statement.</param>
        /// <returns>The formatted log statement.</returns>
        private string FormatLogStatement(string value) =>
            string.Format(CultureInfo.InvariantCulture, "[Transfer:{0}] {1}", _transferId, value);
    }
}