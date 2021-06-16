// Copyright (c) Microsoft Corporation.
// Licensed under the MIT license.

namespace Microsoft.InnerEye.DicomConstraints
{
    using System;
    using Dicom;

    /// <summary>
    /// static class to compute a constraint value
    /// </summary>
    /// <typeparam name="TCompare">Instance of IComparable.</typeparam>
    /// <typeparam name="TExtract">Type of value extracted from DICOM tag.</typeparam>
    /// <typeparam name="TSelector">Instance of ISelector.</typeparam>
    internal static class BaseOrderConstraint<TCompare, TExtract, TSelector>
        where TCompare : IComparable, new()
        where TSelector : ISelector<TExtract, TCompare>, new()
    {
        /// <summary>
        /// Runs the ordering constraint
        /// </summary>
        /// <param name="dataSet">DICOM dataset to test.</param>
        /// <param name="order">Order between value in DICOM tag and v.</param>
        /// <param name="v">Value to test.</param>
        /// <param name="t">DICOM tag.</param>
        /// <param name="constraint">Source constraint.</param>
        /// <param name="ordinal">Tag ordinal.</param>
        /// <returns>New DicomConstraintResult.</returns>
        public static DicomConstraintResult Check(DicomDataset dataSet, Order order, TCompare v, DicomTag t, DicomConstraint constraint, int ordinal = 0)
        {
            try
            {
                // Predicate function to compare the value in the dataSet with v
                Func<TExtract, bool> predicate = datasetValue =>
                {
                    // Compare v with datasetValue. This is logically the wrong way round but v may have
                    // an override of CompareTo. For example see OrderedString.CompareTo.
                    var r = v.CompareTo(new TSelector().SelectValue(datasetValue));
                    // CompareTo is < 0 | 0 | > 0
                    // < 0 means that v precedes datasetValue in sort order, i.e. datasetValue follows v
                    // > 0 means that v follows datasetValue in sort order, i.e. datasetValue predeces v
                    // = 0 otherwise
                    // Reverse the ordering to get datasetValue compared with v.
                    r *= -1;
                    // Map onto Order
                    var ro = (r < 0) ? Order.LessThan : (r > 0) ? Order.GreaterThan : Order.Equal;

                    return (ro & order) != 0;
                };

                // Check will throw if the tag is not there.
                return DicomConstraintResult.Check(dataSet, t, ordinal, predicate, constraint);
            }
            // Catch if we have tried to parse the value into the wrong type and return false
            catch (FormatException)
            {
                return new DicomConstraintResult(false, constraint);
            }
        }
    }
}