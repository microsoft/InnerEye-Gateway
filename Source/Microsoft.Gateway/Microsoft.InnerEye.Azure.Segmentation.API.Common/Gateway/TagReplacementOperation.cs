﻿// Copyright (c) Microsoft Corporation.
// Licensed under the MIT license.

namespace Microsoft.InnerEye.Azure.Segmentation.API.Common
{
    /// <summary>
    /// TagReplacement operation
    /// </summary>
    public enum TagReplacementOperation
    {
        /// <summary>
        /// Update
        /// </summary>
        UpdateIfExists,

        /// <summary>
        /// The append if exists
        /// </summary>
        AppendIfExists,
    }
}
