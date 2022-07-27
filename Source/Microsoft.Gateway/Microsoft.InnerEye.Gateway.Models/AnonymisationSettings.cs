// Copyright (c) Microsoft Corporation.
// Licensed under the MIT license.

namespace Microsoft.InnerEye.Gateway.Models
{
    using System;
    using System.Linq;
    using System.Collections.Generic;


    public class AnonymisationSettings : IEquatable<AnonymisationSettings>
    {

        // list of tags and sending protocol to attach to anonymous data
        private readonly Dictionary<string, IEnumerable<string>> _dicomTagsAnonymisationConfig;

        /// <summary>
        /// Initializes a new instance of the <see cref="AnonymisationSettings"/> class.
        /// </summary>  
        public AnonymisationSettings(Dictionary<string, IEnumerable<string>> dicomTagsAnonymisationConfig)
        {
            _dicomTagsAnonymisationConfig = dicomTagsAnonymisationConfig ?? throw new ArgumentNullException(nameof(dicomTagsAnonymisationConfig));
        }

        public Dictionary<string, IEnumerable<string>> DicomTagsAnonymisationConfig => _dicomTagsAnonymisationConfig;

        public override bool Equals(object obj)
        {
            return Equals(obj as AnonymisationSettings);
        }

        /// <inheritdoc/>
        public bool Equals(AnonymisationSettings other)
        {

            var equal = true;
            foreach (var entry in DicomTagsAnonymisationConfig)
            {
                if (other != null && other.DicomTagsAnonymisationConfig.ContainsKey(entry.Key))
                {
                    if (!Enumerable.SequenceEqual(other.DicomTagsAnonymisationConfig[entry.Key], entry.Value))
                    {
                        equal = false;
                        break;
                    }
                }
                else
                {
                    equal = false;
                    break;
                }
            }

            return equal;
        }

        /// <inheritdoc/>
        public override int GetHashCode()
        {
            var hashCode = 1943766103;
            hashCode = hashCode * -1521134295 + EqualityComparer<Dictionary<string, IEnumerable<string>>>.Default.GetHashCode(DicomTagsAnonymisationConfig);
            return hashCode;
        }
    }
}
