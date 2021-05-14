// Copyright (c) Microsoft Corporation.
// Licensed under the MIT license.

namespace Microsoft.InnerEye.Listener.DataProvider.Models
{
    /// <summary>
    /// List of progress codes for receiving data over Dicom.
    /// </summary>
    public enum DicomReceiveProgressCode
    {
        FileReceived,

        ErrorSavingFile,

        ErrorCouldNotUnderstand,

        GenericStorageException,

        TransferAborted,

        AssociationEstablished,

        AssociationReleased,

        ConnectionClosed,

        Echo,
    }
}