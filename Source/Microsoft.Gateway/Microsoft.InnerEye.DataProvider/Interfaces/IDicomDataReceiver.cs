// Copyright (c) Microsoft Corporation.
// Licensed under the MIT license.

namespace Microsoft.InnerEye.Listener.DataProvider.Interfaces
{
    using System;
    using System.Collections.Generic;

    using Dicom;

    using Models;

    /// <summary>
    /// The Dicom server creator.
    /// </summary>
    public interface IDicomDataReceiver : IDisposable
    {
        /// <summary>
        /// Gets if the server is currently listening.
        /// </summary>
        bool IsListening { get; }

        /// <summary>
        /// Data received event - this can be called from multiple different threads.
        /// </summary>
        event EventHandler<DicomDataReceiverProgressEventArgs> DataReceived;

        /// <summary>
        /// Starts the Dicom server using the supplied dicom application entity.
        /// </summary>
        /// <param name="port">
        /// The port to start listening on. 
        /// Note, currently we always listens on Ipv4.Any and do not use DcmOwnAe.Title or DcmOwnAe.IpAddress. 
        /// </param>
        /// <param name="getAcceptedTransferSyntaxes">
        /// The function for getting the accepted transfer syntaxes when an association is made.
        /// </param>
        /// <param name="timeout">
        /// The time we will wait for the server to start listening.
        /// Recommended timeout is 2 seconds.
        /// </param>
        /// <returns>If we are listening.</returns>
        bool StartServer(int port, Func<IReadOnlyDictionary<DicomUID, DicomTransferSyntax[]>> getAcceptedTransferSyntaxes, TimeSpan timeout);

        /// <summary>
        /// Stops the server.
        /// </summary>
        void StopServer();
    }
}