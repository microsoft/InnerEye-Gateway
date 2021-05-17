// Copyright (c) Microsoft Corporation.
// Licensed under the MIT license.

namespace Microsoft.InnerEye.Listener.Tests.Models
{
    using System.Collections.Generic;
    using Microsoft.InnerEye.Listener.Common;

    /// <summary>
    /// Mock implementation of AETConfigModels provider.
    /// </summary>
    public class MockAETConfigProvider
    {
        private readonly IEnumerable<AETConfigModel> _aetConfigModels;

        public IEnumerable<AETConfigModel> AETConfigModels() => _aetConfigModels;

        /// <summary>
        /// Initializes a new instance of the <see cref="MockAETConfigProvider"/> class.
        /// </summary>
        /// <param name="aetConfigModel">Single AETConfigModel for the list.</param>
        public MockAETConfigProvider(AETConfigModel aetConfigModel)
        {
            _aetConfigModels = new[] { aetConfigModel };
        }
    }
}
