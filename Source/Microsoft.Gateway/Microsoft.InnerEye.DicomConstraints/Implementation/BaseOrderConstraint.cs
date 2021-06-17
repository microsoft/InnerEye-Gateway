// Copyright (c) Microsoft Corporation.
// Licensed under the MIT license.

namespace Microsoft.InnerEye.DicomConstraints
{
    using System;
    using Dicom;

    /// <summary>
    /// Static helper class to compute Check for order style constraints.
    /// </summary>
    internal static class BaseOrderConstraint
    {
        /// <summary>
        /// Check an order style constraint against a DICOM dataset when no DICOM tag value transformation required.
        /// </summary>
        /// <param name="dataSet">DICOM dataset to test.</param>
        /// <param name="dicomOrderedTag">DicomOrderedTag to use for test.</param>
        /// <param name="constraint">Source <see cref="DicomTagConstraint"/>.</param>
        /// <typeparam name="T">Type of object extracted from DICOM tag.</typeparam>
        /// <returns>New DicomConstraintResult.</returns>
        public static DicomConstraintResult Check<T>(
            DicomDataset dataSet, DicomOrderedTag<T> dicomOrderedTag, DicomTagConstraint constraint)
            where T : IComparable =>
                Check<T, T, DefaultSelector<T>>(dataSet, dicomOrderedTag.Order, dicomOrderedTag.Value, constraint.Index.DicomTag, constraint, dicomOrderedTag.Ordinal);

        /// <summary>
        /// Check an order style constraint against a DICOM dataset when a DICOM tag value transformation is required.
        /// </summary>
        /// <param name="dataSet">DICOM dataset to test.</param>
        /// <param name="dicomOrderedTag">DicomOrderedTag to use for test.</param>
        /// <param name="constraint">Source <see cref="DicomTagConstraint"/>.</param>
        /// <typeparam name="TSelection">Type of object selected from TSource.</typeparam>
        /// <typeparam name="TSource">Type of value extracted from DICOM tag.</typeparam>
        /// <typeparam name="TSelector">Instance of ISelector to map from a TSource extracted from DICOM tag to a TSelection.</typeparam>
        /// <returns>New DicomConstraintResult.</returns>
        public static DicomConstraintResult Check<TSelection, TSource, TSelector>(
            DicomDataset dataSet, DicomOrderedTag<TSelection> dicomOrderedTag, DicomTagConstraint constraint)
            where TSelection : IComparable
            where TSelector : ISelector<TSource, TSelection>, new() =>
                Check<TSelection, TSource, TSelector>(dataSet, dicomOrderedTag.Order, dicomOrderedTag.Value, constraint.Index.DicomTag, constraint, dicomOrderedTag.Ordinal);

        /// <summary>
        /// Check an order style constraint against a DICOM dataset.
        /// </summary>
        /// <param name="dataSet">DICOM dataset to test.</param>
        /// <param name="order">Target order result of applying comparable to value in DICOM tag (after transformation by TSelector).</param>
        /// <param name="comparable">Comparable to use for test.</param>
        /// <param name="dicomTag">DICOM tag to test in dataSet.</param>
        /// <param name="constraint">Source <see cref="DicomConstraint"/>.</param>
        /// <param name="ordinal">Tag ordinal.</param>
        /// <remarks>
        /// The ordinal is usually 0. However if the value multiplicity for the DICOM tag is > 1 then:
        /// If ordinal >= 0 then the test is applied to that ordinal.
        /// Otherwise the test is applied to all ordinals, and returns true if the test passes for any of the ordinals.
        /// </remarks>
        /// <returns>New DicomConstraintResult.</returns>
        public static DicomConstraintResult Check<TSelection, TSource, TSelector>(
            DicomDataset dataSet, Order order, IComparable comparable, DicomTag dicomTag, DicomConstraint constraint, int ordinal)
            where TSelector : ISelector<TSource, TSelection>, new()
        {
            try
            {
                // Predicate function to apply comparable to the value in the dataSet tag.
                Func<TSource, bool> predicate = datasetValue =>
                {
                    // Apply comparable to the (transformed) datasetValue. This is logically the wrong way round but comparable may have
                    // an override of CompareTo. For example see OrderedString.CompareTo.
                    var r = comparable.CompareTo(new TSelector().SelectValue(datasetValue));
                    // CompareTo is < 0 | 0 | > 0
                    // < 0 means that v precedes datasetValue in sort order, i.e. datasetValue follows v
                    // > 0 means that v follows datasetValue in sort order, i.e. datasetValue precedes v
                    // = 0 otherwise
                    // Reverse the ordering to get datasetValue compared with comparable.
                    r *= -1;
                    // Map onto Order
                    var ro = (r < 0) ? Order.LessThan : (r > 0) ? Order.GreaterThan : Order.Equal;

                    // Mask this with the target order and return true if at least one flag matches.
                    return (ro & order) != 0;
                };

                // Check will throw if the tag is not there.
                return DicomConstraintResult.Check(dataSet, dicomTag, ordinal, predicate, constraint);
            }
            catch (InvalidCastException)
            {
                // Catch if we have tried to parse the value into the wrong type and return false

                return new DicomConstraintResult(false, constraint);
            }
        }
    }
}
