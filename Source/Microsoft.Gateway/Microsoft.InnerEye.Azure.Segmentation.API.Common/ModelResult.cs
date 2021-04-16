namespace Microsoft.InnerEye.Azure.Segmentation.API.Common
{
    using Dicom;

    using System;

    /// <summary>
    /// Model result information.
    /// </summary>
    public class ModelResult : IEquatable<ModelResult>
    {
        /// <summary>
        /// Constructor
        /// </summary>
        /// <param name="progress"></param>
        /// <param name="error"></param>
        /// <param name="dicomResult"></param>
        public ModelResult(int progress, string error, DicomFile dicomResult)
        {
            Progress = progress;
            Error = error ?? throw new ArgumentNullException(nameof(error));
            DicomResult = dicomResult;
        }

        /// <summary>
        /// Gets or sets progress.
        /// </summary>
        public int Progress { get; }

        /// <summary>
        /// Gets error message.
        /// </summary>
        public string Error { get; }

        /// <summary>
        /// Gets the dicom result
        /// </summary>
        public DicomFile DicomResult { get; }

        /// <inheritdoc/>
        public bool Equals(ModelResult other)
        {
            if (other == null)
            {
                return false;
            }
            else
            {
                return Progress == other.Progress && Error == other.Error && DicomResult == other.DicomResult;
            }
        }

        /// <inheritdoc/>
        public override bool Equals(object obj)
        {
            var progressReport = obj as ModelResult;

            if (progressReport == null)
            {
                return false;
            }

            return Equals(progressReport);
        }

        /// <inheritdoc/>
        public override string ToString()
        {
            return $"Progress = {Progress}, Error= {Error}, DicomResult={DicomResult}";
        }

        /// <inheritdoc/>
        public override int GetHashCode()
        {
            return base.GetHashCode();
        }
    }
}
