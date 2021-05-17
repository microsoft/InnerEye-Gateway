// Copyright (c) Microsoft Corporation.
// Licensed under the MIT license.

namespace Microsoft.InnerEye.DicomConstraints
{
    /// <summary>
    /// Interface to select an element TSelection from an instance of type TSource
    /// </summary>
    /// <typeparam name="TSource"></typeparam>
    /// <typeparam name="TSelection"></typeparam>
    internal interface ISelector<TSource, TSelection>
    {
        /// <summary>
        /// Select a value
        /// </summary>
        /// <param name="source"></param>
        /// <returns></returns>
        TSelection SelectValue(TSource source);
    }
}
