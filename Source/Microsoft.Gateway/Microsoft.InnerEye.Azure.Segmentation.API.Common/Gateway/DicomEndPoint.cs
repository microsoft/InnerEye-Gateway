namespace Microsoft.InnerEye.Azure.Segmentation.API.Common
{
    using System;
    using System.Collections.Generic;
    using System.ComponentModel.DataAnnotations;

    /// <summary>
    /// DicomEndPoint
    /// </summary>
    public class DicomEndPoint : IEquatable<DicomEndPoint>
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="DicomEndPoint"/> class.
        /// </summary>
        /// <param name="title">The title.</param>
        /// <param name="port">The port.</param>
        /// <param name="ip">The ip.</param>
        /// <exception cref="ArgumentNullException">
        /// title
        /// or
        /// ip
        /// </exception>
        /// <exception cref="ArgumentException">The specified Ip is empty - ip</exception>
        public DicomEndPoint(string title, int port, string ip)
        {
            Title = title ?? throw new ArgumentNullException(nameof(title));
            Port = port;
            Ip = ip ?? throw new ArgumentNullException(nameof(ip));
            if (string.IsNullOrEmpty(Ip))
            {
                throw new ArgumentException("The specified Ip is empty", nameof(ip));
            }
        }

        /// <summary>
        /// The DICOM application entity title of the Store-SCP
        /// </summary>
        [Required]
        [StringLength(16)]
        public string Title { get; }

        /// <summary>
        /// The port number of the Store-SCP
        /// </summary>
        [Required]
        [Range(1, 65535)]
        public int Port { get; }

        /// <summary>
        /// The IP address of the Store-SCP
        /// </summary>
        [Required]
        public string Ip { get; }

        /// <inheritdoc/>
        public override bool Equals(object obj)
        {
            return Equals(obj as DicomEndPoint);
        }

        /// <inheritdoc/>
        public bool Equals(DicomEndPoint other)
        {
            return other != null &&
                   Title == other.Title &&
                   Port == other.Port &&
                   Ip == other.Ip;
        }

        /// <inheritdoc/>
        public override int GetHashCode()
        {
            var hashCode = 1692922603;
            hashCode = hashCode * -1521134295 + EqualityComparer<string>.Default.GetHashCode(Title);
            hashCode = hashCode * -1521134295 + Port.GetHashCode();
            hashCode = hashCode * -1521134295 + EqualityComparer<string>.Default.GetHashCode(Ip);
            return hashCode;
        }

        /// <inheritdoc/>
        public static bool operator ==(DicomEndPoint left, DicomEndPoint right)
        {
            return EqualityComparer<DicomEndPoint>.Default.Equals(left, right);
        }

        /// <inheritdoc/>
        public static bool operator !=(DicomEndPoint left, DicomEndPoint right)
        {
            return !(left == right);
        }
    }
}
