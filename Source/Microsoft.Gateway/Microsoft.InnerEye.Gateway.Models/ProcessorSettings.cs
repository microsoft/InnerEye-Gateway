namespace Microsoft.InnerEye.Gateway.Models
{
    using System;
    using System.Collections.Generic;

    /// <summary>
    /// ProcessorSettings class
    /// </summary>
    public class ProcessorSettings : IEquatable<ProcessorSettings>
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="ProcessorSettings"/> class.
        /// </summary>
        /// <param name="licenseKeyEnvVar">License key environment variable.</param>
        /// <param name="InferenceUri">Inference API Uri.</param>
        public ProcessorSettings(
            string licenseKeyEnvVar,
            Uri inferenceUri)
        {
            LicenseKeyEnvVar = licenseKeyEnvVar;
            InferenceUri = inferenceUri;
        }

        /// <summary>
        /// Clone this into a new instance of the <see cref="ProcessorSettings"/> class, optionally replacing some properties.
        /// </summary>
        /// <param name="licenseKeyEnvVar">Optional new license key environment variable.</param>
        /// <param name="InferenceUri">Optional new inference API Uri.</param>
        /// <returns>New ProcessorSettings.</returns></remarks>
        public ProcessorSettings With(
            string licenseKeyEnvVar = null,
            Uri inferenceUri = null) =>
                new ProcessorSettings(
                    !string.IsNullOrWhiteSpace(licenseKeyEnvVar) ? licenseKeyEnvVar : LicenseKeyEnvVar,
                    inferenceUri ?? InferenceUri);

        /// <summary>
        /// Gets the license key environment variable.
        /// </summary>
        public string LicenseKeyEnvVar { get; }

        /// <summary>
        /// Gets the inference API Uri.
        /// </summary>
        public Uri InferenceUri { get; }

        /// <inheritdoc/>
        public override bool Equals(object obj)
        {
            return Equals(obj as ProcessorSettings);
        }

        /// <inheritdoc/>
        public bool Equals(ProcessorSettings other)
        {
            return other != null &&
                   LicenseKeyEnvVar == other.LicenseKeyEnvVar &&
                   EqualityComparer<Uri>.Default.Equals(InferenceUri, other.InferenceUri);
        }

        /// <inheritdoc/>
        public override int GetHashCode()
        {
            var hashCode = 1943766103;
            hashCode = hashCode * -1521134295 + EqualityComparer<string>.Default.GetHashCode(LicenseKeyEnvVar);
            hashCode = hashCode * -1521134295 + EqualityComparer<Uri>.Default.GetHashCode(InferenceUri);
            return hashCode;
        }

        /// <inheritdoc/>
        public static bool operator ==(ProcessorSettings left, ProcessorSettings right)
        {
            return EqualityComparer<ProcessorSettings>.Default.Equals(left, right);
        }

        /// <inheritdoc/>
        public static bool operator !=(ProcessorSettings left, ProcessorSettings right)
        {
            return !(left == right);
        }
    }
}
