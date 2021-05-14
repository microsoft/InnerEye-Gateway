// Copyright (c) Microsoft Corporation.
// Licensed under the MIT license.

namespace Microsoft.InnerEye.Gateway.Models
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using Dicom;
    using Microsoft.InnerEye.Azure.Segmentation.API.Common;
    using Newtonsoft.Json;

    /// <summary>
    /// Model containing all configuration for the receive service.
    /// </summary>
    public class ReceiveServiceConfig : IEquatable<ReceiveServiceConfig>
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="ReceiveServiceConfig"/> class.
        /// </summary>
        /// <param name="gatewayDicomEndPoint">The gateway dicom end point.</param>
        /// <param name="rootDicomFolder">The root dicom folder.</param>
        /// <param name="acceptedSopClassesAndTransferSyntaxesUIDs">The accepted sop classes and transfer syntaxes UI ds.</param>
        /// <exception cref="ArgumentNullException">
        /// gatewayDicomEndPoint
        /// or
        /// rootDicomFolder
        /// or
        /// acceptedSopClassesAndTransferSyntaxesUIDs
        /// </exception>
        public ReceiveServiceConfig(
            DicomEndPoint gatewayDicomEndPoint,
            string rootDicomFolder,
            Dictionary<string, string[]> acceptedSopClassesAndTransferSyntaxesUIDs)
        {
            GatewayDicomEndPoint = gatewayDicomEndPoint ?? throw new ArgumentNullException(nameof(gatewayDicomEndPoint));
            RootDicomFolder = rootDicomFolder ?? throw new ArgumentNullException(nameof(rootDicomFolder));
            AcceptedSopClassesAndTransferSyntaxesUIDs = acceptedSopClassesAndTransferSyntaxesUIDs ?? throw new ArgumentNullException(nameof(acceptedSopClassesAndTransferSyntaxesUIDs));

            if (AcceptedSopClassesAndTransferSyntaxesUIDs.Count == 0)
            {
                throw new ArgumentException("The GatewayReceiveConfiguration must have at least 1 acceptable SOPClassUID", nameof(acceptedSopClassesAndTransferSyntaxesUIDs));
            }

            if (AcceptedSopClassesAndTransferSyntaxesUIDs.Any(kvp => kvp.Value.Length == 0))
            {
                throw new ArgumentException("Every SopClassUID must have at least 1 supported Transfer Syntax", nameof(acceptedSopClassesAndTransferSyntaxesUIDs));
            }
        }

        /// <summary>
        /// Clone this into a new instance of the <see cref="ReceiveServiceConfig"/> class.
        /// </summary>
        /// <param name="gatewayDicomEndPoint">The gateway dicom end point.</param>
        /// <param name="rootDicomFolder">The root dicom folder.</param>
        /// <param name="acceptedSopClassesAndTransferSyntaxesUIDs">The accepted sop classes and transfer syntaxes UI ds.</param>
        public ReceiveServiceConfig With(
            DicomEndPoint gatewayDicomEndPoint = null,
            string rootDicomFolder = null,
            Dictionary<string, string[]> acceptedSopClassesAndTransferSyntaxesUIDs = null) =>
                new ReceiveServiceConfig(
                    gatewayDicomEndPoint ?? GatewayDicomEndPoint,
                    rootDicomFolder ?? RootDicomFolder,
                    acceptedSopClassesAndTransferSyntaxesUIDs ?? AcceptedSopClassesAndTransferSyntaxesUIDs);

        /// <summary>
        /// Gets GatewayDicomEndPoint
        /// </summary>
        public DicomEndPoint GatewayDicomEndPoint { get; }

        /// <summary>
        /// Gets the root Dicom folder where we store data.
        /// </summary>
        public string RootDicomFolder { get; }

        /// <summary>
        /// Gets the accepted Sop classes and transfer syntaxes.
        /// </summary>
        [JsonIgnore]
        public Dictionary<DicomUID, DicomTransferSyntax[]> AcceptedSopClassesAndTransferSyntaxes =>
            AcceptedSopClassesAndTransferSyntaxesUIDs.ToDictionary(
                keyValue => DicomUID.Parse(keyValue.Key),
                keyValue => keyValue.Value.Select(x => DicomTransferSyntax.Parse(x)).ToArray());

        /// <summary>
        /// Gets the accepted Sop classes and transfer syntaxes.
        /// </summary>
        public Dictionary<string, string[]> AcceptedSopClassesAndTransferSyntaxesUIDs { get; }

        /// <inheritdoc/>
        public override bool Equals(object obj)
        {
            return Equals(obj as ReceiveServiceConfig);
        }

        /// <inheritdoc/>
        public bool Equals(ReceiveServiceConfig other)
        {
            return other != null &&
                   EqualityComparer<DicomEndPoint>.Default.Equals(GatewayDicomEndPoint, other.GatewayDicomEndPoint) &&
                   RootDicomFolder == other.RootDicomFolder &&
                   CompareAcceptedSopClassesAndTransferSyntaxesUIDs(AcceptedSopClassesAndTransferSyntaxesUIDs, other.AcceptedSopClassesAndTransferSyntaxesUIDs);
        }

        /// <summary>
        /// Compare the acceptedSopClassesAndTransferSyntaxesUIDs.
        /// </summary>
        /// <param name="left">Left object to compare.</param>
        /// <param name="right">Right object to compare.</param>
        /// <returns>True if equal, false otherwise.</returns>
        private static bool CompareAcceptedSopClassesAndTransferSyntaxesUIDs(Dictionary<string, string[]> left, Dictionary<string, string[]> right)
        {
            return left.Count == right.Count &&
                    left.Keys.All(key => right.ContainsKey(key) && left[key].SequenceEqual(right[key]));
        }

        /// <inheritdoc/>
        public override int GetHashCode()
        {
            var hashCode = 588952872;
            hashCode = hashCode * -1521134295 + EqualityComparer<DicomEndPoint>.Default.GetHashCode(GatewayDicomEndPoint);
            hashCode = hashCode * -1521134295 + EqualityComparer<string>.Default.GetHashCode(RootDicomFolder);
            hashCode = hashCode * -1521134295 + EqualityComparer<Dictionary<string, string[]>>.Default.GetHashCode(AcceptedSopClassesAndTransferSyntaxesUIDs);
            return hashCode;
        }

        /// <inheritdoc/>
        public static bool operator ==(ReceiveServiceConfig left, ReceiveServiceConfig right)
        {
            return EqualityComparer<ReceiveServiceConfig>.Default.Equals(left, right);
        }

        /// <inheritdoc/>
        public static bool operator !=(ReceiveServiceConfig left, ReceiveServiceConfig right)
        {
            return !(left == right);
        }
    }
}
