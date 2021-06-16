// Copyright (c) Microsoft Corporation.
// Licensed under the MIT license.

namespace Microsoft.InnerEye.DicomConstraints
{
    using System;
    using System.Linq;
    using Dicom;

    /// <summary>
    /// static class to compute a constraint value
    /// </summary>
    /// <typeparam name="TCompare"></typeparam>
    /// <typeparam name="TExtract"></typeparam>
    /// <typeparam name="TSelector"></typeparam>
    internal static class BaseOrderConstraint<TCompare, TExtract, TSelector>
        where TCompare : IComparable, new()
        where TSelector : ISelector<TExtract, TCompare>, new()
    {
        /// <summary>
        /// Runs the ordering constraint
        /// </summary>
        /// <param name="dataSet"></param>
        /// <param name="order"></param>
        /// <param name="v"></param>
        /// <param name="t"></param>
        /// <param name="constraint"></param>
        /// <param name="ordinal"></param>
        /// <returns></returns>
        public static DicomConstraintResult Check(DicomDataset dataSet, Order order, TCompare v, DicomTag t, DicomConstraint constraint, int ordinal = 0)
        {
            if (dataSet == null)
            {
                throw new ArgumentNullException(nameof(dataSet));
            }

            Func<TExtract, bool> predicate = datasetValue =>
            {
                var selector = new TSelector();

                // CompareTo is < 0 | 0 | > 0
                var r = v.CompareTo(selector.SelectValue(datasetValue));
                var ro = (r < 0) ? Order.GreaterThan : (r > 0) ? Order.LessThan : Order.Equal;

                return (ro & order) != 0;
            };

            try
            {
                if (ordinal >= 0)
                {
                    // Get will throw if the tag is not there.
                    var datasetValue = dataSet.GetValue<TExtract>(t, ordinal);

                    return new DicomConstraintResult(predicate(datasetValue), constraint);
                }
                else
                {
                    // Get will throw if the tag is not there.
                    var datasetValues = dataSet.GetValues<TExtract>(t);

                    return new DicomConstraintResult(datasetValues.Any(predicate), constraint);
                }
            }

            // Catch if we have tried to parse the value into the wrong type and return false
            catch (FormatException)
            {
                return new DicomConstraintResult(false, constraint);
            }
        }
    }
}