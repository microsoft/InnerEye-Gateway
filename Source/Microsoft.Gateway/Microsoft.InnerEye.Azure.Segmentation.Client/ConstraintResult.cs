namespace Microsoft.InnerEye.Azure.Segmentation.Client
{
    using System.Collections.Generic;
    using System.Linq;

    using Dicom;

    using Microsoft.InnerEye.DicomConstraints;

    /// <summary>
    /// The constraint result.
    /// </summary>
    public class ConstraintResult<T>
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="ConstraintResult{T}"/> class.
        /// </summary>
        /// <param name="dicomConstraintResults">The dicom constraint results.</param>
        public ConstraintResult(IEnumerable<DicomConstraintResult> dicomConstraintResults)
        {
            Matched = false;
            DicomConstraintResults = dicomConstraintResults;
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="ConstraintResult{T}"/> class.
        /// </summary>
        /// <param name="dicomConstraintResults">The dicom constraint results.</param>
        /// <param name="result">The result.</param>
        public ConstraintResult(IEnumerable<DicomConstraintResult> dicomConstraintResults, T result)
        {
            Matched = true;
            DicomConstraintResults = dicomConstraintResults;
            Result = result;
        }

        /// <summary>
        /// Gets a value indicating whether this <see cref="ConstraintResult{T}"/> is matched.
        /// </summary>
        /// <value>
        ///   <c>true</c> if matched; otherwise, <c>false</c>.
        /// </value>
        public bool Matched { get; }

        /// <summary>
        /// Gets the dicom constraint results per Dicom series.
        /// </summary>
        /// <value>
        /// The dicom constraint results per Dicom series.
        /// </value>
        public IEnumerable<DicomConstraintResult> DicomConstraintResults { get; }

        /// <summary>
        /// Gets the result.
        /// </summary>
        /// <value>
        /// The result.
        /// </value>
        public T Result { get; }

        /// <summary>
        /// Gets the Dicom tags from constraint results that have the specified result value.
        /// </summary>
        /// <param name="constraintResult">if set to <c>true</c> [constraint result].</param>
        /// <returns>The collection of Dicom tags for all constraints the match the result.</returns>
        public IEnumerable<DicomTag> GetDicomConstraintsDicomTags(bool constraintResult = false)
        {
            return GetDicomConstraintsDicomTags(constraintResult, DicomConstraintResults.ToArray()).Distinct();
        }

        /// <summary>
        /// Recursively gets all child constraints with the result specified and returns the Dicom tag represented by this constraint.
        /// </summary>
        /// <param name="constraintResult">if set to <c>true</c> [constraint result].</param>
        /// <param name="dicomConstraintResult">The dicom constraint result.</param>
        /// <returns>The collection of Dicom tags from the constraint results for the specified result type.</returns>
        private static IEnumerable<DicomTag> GetDicomConstraintsDicomTags(bool constraintResult, IReadOnlyList<DicomConstraintResult> dicomConstraintResult)
        {
            var result = new List<DicomTag>();

            foreach (var item in dicomConstraintResult)
            {
                if (item.Result == constraintResult)
                {
                    if (item.ChildResults == null)
                    {
                        switch (item.Constraint)
                        {
                            case DicomTagConstraint tagConstraint:
                                {
                                    result.Add(tagConstraint.Index.DicomTag);
                                    break;
                                }

                            case RequiredTagConstraint requiredTag:
                                {
                                    result.Add(requiredTag.Constraint.Index.DicomTag);
                                    break;
                                }
                        }
                    }
                    else
                    {
                        result.AddRange(GetDicomConstraintsDicomTags(constraintResult, item.ChildResults));
                    }
                }
            }

            return result;
        }
    }
}