// Copyright (c) Microsoft Corporation.
// Licensed under the MIT license.

namespace Microsoft.InnerEye.Listener.Common
{
    using System;
    using System.Collections.Generic;
    using Microsoft.InnerEye.Azure.Segmentation.API.Common;

    /// <summary>
    /// Custom Model Class to get the values of ClientAETConfig based on the 
    /// values of CalledAET and Calling AET, from the json configuration
    /// </summary>
    public class AETConfigModel : IEquatable<AETConfigModel>
    {
        /// <summary>
        /// Gets the Called Application Entity Title
        /// </summary>
        public string CalledAET { get; }

        /// <summary>
        /// Gets the Calling Application Entity Title
        /// </summary>
        public string CallingAET { get; }

        /// <summary>
        /// Encodes how an AET is configured
        /// </summary>
        public ClientAETConfig AETConfig { get; }

        /// <summary>
        /// Initialize a new instance of the <see cref="AETConfigModel"/> class.
        /// </summary>
        /// <param name="calledAET">Called application entity title.</param>
        /// <param name="callingAET">Calling application entity title.</param>
        /// <param name="aetConfig">AET config.</param>
        public AETConfigModel(
            string calledAET,
            string callingAET,
            ClientAETConfig aetConfig)
        {
            CalledAET = calledAET;
            CallingAET = callingAET;
            AETConfig = aetConfig;
        }

        /// <summary>
        /// Clone this into a new instance of the <see cref="AETConfigModel"/> class, optionally replacing some properties.
        /// </summary>
        /// <param name="calledAET">Optional new CalledAET.</param>
        /// <param name="callingAET">Optional new CallingAET.</param>
        /// <param name="aetConfig">Optional new AETConfig.</param>
        /// <returns>New AETConfigModel.</returns>
        public AETConfigModel With(
            string calledAET = null,
            string callingAET = null,
            ClientAETConfig aetConfig = null) =>
                new AETConfigModel(
                    calledAET ?? CalledAET,
                    callingAET ?? CallingAET,
                    aetConfig ?? AETConfig);

        /// <inheritdoc/>
        public override bool Equals(object obj)
        {
            return Equals(obj as AETConfigModel);
        }

        /// <inheritdoc/>
        public bool Equals(AETConfigModel other)
        {
            return other != null &&
                   CalledAET == other.CalledAET &&
                   CallingAET == other.CallingAET &&
                   EqualityComparer<ClientAETConfig>.Default.Equals(AETConfig, other.AETConfig);
        }

        /// <inheritdoc/>
        public override int GetHashCode()
        {
            return HashCode.Combine(CalledAET, CallingAET, AETConfig);
        }

        /// <inheritdoc/>
        public static bool operator ==(AETConfigModel left, AETConfigModel right)
        {
            return EqualityComparer<AETConfigModel>.Default.Equals(left, right);
        }

        /// <inheritdoc/>
        public static bool operator !=(AETConfigModel left, AETConfigModel right)
        {
            return !(left == right);
        }
    }
}
