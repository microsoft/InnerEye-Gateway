namespace Microsoft.InnerEye.DicomConstraints
{
    using System;
    using System.ComponentModel.DataAnnotations;
    using Dicom;
    using Newtonsoft.Json;

    /// <summary>
    /// The ordering constraint takes a Dicom tag and an "ordering" map from the tag value to the template type
    /// T and exposes an ordering constraint through DicomTagConstraint.
    /// </summary>
    [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Performance", "CA1812:AvoidUninstantiatedInternalClasses", Justification = "TBD")]
    internal class OrderingConstraint<T> : DicomTagConstraint
        where T : IComparable, new()
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="OrderingConstraint{T}"/> class.
        /// This constructor is for json serialization only. Use the alternative construtor.
        /// </summary>
        /// <param name="index">The Dicmo tag index you wish to constrain by the function.</param>
        /// <param name="function">The constraint function.</param>
        /// <exception cref="ArgumentNullException">function</exception>
        [JsonConstructor]
        public OrderingConstraint(DicomTagIndex index, DicomOrderedTag<T> function)
            : base(index)
        {
            Function = function ?? throw new ArgumentNullException(nameof(function));
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="OrderingConstraint{T}"/> class.
        /// </summary>
        /// <remarks>
        /// It must be possible to extract type T from the given tag. If not an exception will be
        /// thrown during a call to Check(...)
        /// </remarks>
        /// <param name="order">The ordering constraint you wish to impose.</param>
        /// <param name="value">The value to use in the right hand side of the ordering constraint.</param>
        /// <param name="tag">The tag you wish to constrain.</param>
        /// <param name="ordinal">The ordinal index of the tag value you wish to constrain (for multi-value tag types).</param>
        public OrderingConstraint(Order order, T value, DicomTag tag, int ordinal = 0)
            : this(new DicomTagIndex(tag), new DicomOrderedTag<T>(order, value, ordinal))
        {
        }

        /// <summary>
        /// Ordering function parameters
        /// </summary>
        [Required]
        public DicomOrderedTag<T> Function { get; }

        /// <summary>
        /// Checks that the tag in the given dataset satiisfies the ordering function
        /// </summary>
        /// <param name="dataSet"></param>
        /// <returns></returns>
        public override DicomConstraintResult Check(DicomDataset dataSet)
        {
            return BaseOrderConstraint<T, T, DefaultSelector<T>>.
                Check(dataSet, Function.Order, Function.Value, Index.DicomTag, this, Function.Ordinal);
        }
    }
}
