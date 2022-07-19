// Copyright (c) Microsoft Corporation.
// Licensed under the MIT license.

namespace Microsoft.InnerEye.Gateway.Models
{
    using System;
    using System.Collections.Generic;


    public class AnonymisationSettings : IEquatable<AnonymisationSettings>
    {

        // list of tags and sending protocol to attach to anonymous data
        private readonly IEnumerable<Dictionary<string, string>> _dicomTagsAnonymisationConfig;

        /// <summary>
        /// Initializes a new instance of the <see cref="AnonymisationSettings"/> class.
        /// </summary>  
        public AnonymisationSettings(Dictionary<string, string>[] dicomTagsAnonymisationConfig)
        {
            _dicomTagsAnonymisationConfig = dicomTagsAnonymisationConfig ?? throw new ArgumentNullException(nameof(dicomTagsAnonymisationConfig));
        }

        public IEnumerable<Dictionary<string, string>> DicomTagsAnonymisationConfig => _dicomTagsAnonymisationConfig;

        public override bool Equals(object obj)
        {
            return Equals(obj as AnonymisationSettings);
        }

        /// <inheritdoc/>
        public bool Equals(AnonymisationSettings other)
        {
            return other != null;  /// not sure about this! will return equal even if settings aren't the same?
        }

        /// <inheritdoc/>
        public override int GetHashCode()
        {
            var hashCode = 1943766103;
            hashCode = hashCode * -1521134295 + EqualityComparer<IEnumerable<Dictionary<string, string>>>.Default.GetHashCode(DicomTagsAnonymisationConfig);
            return hashCode;
        }
    }
}
