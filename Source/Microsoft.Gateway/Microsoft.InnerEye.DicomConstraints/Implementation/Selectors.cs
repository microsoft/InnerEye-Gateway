namespace Microsoft.InnerEye.DicomConstraints
{
    using System;
    using Dicom;

    /// <summary>
    /// Default selector - selects the source
    /// </summary>
    [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Performance", "CA1812:AvoidUninstantiatedInternalClasses", Justification = "TBD")]
    internal class DefaultSelector<T> : ISelector<T, T>
    {
        /// <summary>
        /// Constructor
        /// </summary>
        /// <param name="source"></param>
        /// <returns></returns>
        public T SelectValue(T source)
        {
            return source;
        }
    }

    /// <summary>
    /// A generic ISelector implementation that uses IInitializable to convert from TSource to TSelection
    /// </summary>
    /// <typeparam name="TSource"></typeparam>
    /// <typeparam name="TSelection"></typeparam>
    [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Performance", "CA1812:AvoidUninstantiatedInternalClasses", Justification = "TBD")]
    internal class ConvertSelector<TSource, TSelection> : ISelector<TSource, TSelection>
        where TSelection : IInitializable<TSource>, new()
    {
        /// <summary>
        /// some more
        /// </summary>
        /// <param name="source"></param>
        /// <returns></returns>
        public TSelection SelectValue(TSource source)
        {
            var ty = new TSelection();
            ty.Init(source);
            return ty;
        }
    }

    /// <summary>
    /// Selects the timeofday from a DateTime instance
    /// </summary>
    [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Performance", "CA1812:AvoidUninstantiatedInternalClasses", Justification = "TBD")]
    internal class TimeSelector
        : ISelector<DateTime, TimeSpan>
    {
        /// <inheritdoc/>
        public TimeSpan SelectValue(DateTime source)
        {
            return source.TimeOfDay;
        }
    }

    /// <summary>
    /// Extracts an ordered string for comparison from a DicomUID
    /// </summary>
    [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Performance", "CA1812:AvoidUninstantiatedInternalClasses", Justification = "TBD")]
    internal class OrderedUIDStringSelector
        : ISelector<DicomUID, OrderedString>
    {
        /// <summary>
        /// Return the DicomUID as an OrderedString
        /// </summary>
        /// <param name="source"></param>
        /// <returns></returns>
        public OrderedString SelectValue(DicomUID source)
        {
            if (source == null)
            {
                throw new ArgumentNullException(nameof(source));
            }

            return source.UID;
        }
    }
}