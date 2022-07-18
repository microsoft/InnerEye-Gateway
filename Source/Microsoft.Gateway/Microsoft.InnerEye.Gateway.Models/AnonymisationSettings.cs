// Copyright (c) Microsoft Corporation.
// Licensed under the MIT license.

namespace Microsoft.InnerEye.Gateway.Models
{
    using System;
    using System.Collections.Generic;

    using Dicom;

    using Microsoft.InnerEye.Azure.Segmentation.API.Common;


    public class AnonymisationSettings : IEquatable<AnonymisationSettings>
    {

        // list of tags and sending protocol to attach to anonymous data
        private readonly IEnumerable<DicomTagAnonymisation> _dicomTagsAnonymisationConfig;

        /// <summary>
        /// Initializes a new instance of the <see cref="AnonymisationSettings"/> class.
        /// </summary>  
        public AnonymisationSettings(Dictionary<string, string>[] dicomTagsAnonymisationConfig)
        {
            _dicomTagsAnonymisationConfig = dicomTagsAnonymisationConfig != null
                ? ParseAnonymisationTagsConfig(dicomTagsAnonymisationConfig)
                : throw new ArgumentNullException(nameof(dicomTagsAnonymisationConfig));
        }

        private static IEnumerable<DicomTagAnonymisation> ParseAnonymisationTagsConfig(
            Dictionary<string, string>[] dicomTagsAnonymisationConfig)
        {
            var parsedDicomTags = new List<DicomTagAnonymisation>();
            foreach (var dicomTagConfig in dicomTagsAnonymisationConfig)
            {
                AnonymisationMethod anonMethod;

                switch (dicomTagConfig["AnonymisationMethod"])
                {
                    case "Keep":
                        anonMethod = AnonymisationMethod.Keep;
                        break;
                    case "Hash":
                        anonMethod = AnonymisationMethod.Hash;
                        break;
                    case "Random":
                        anonMethod = AnonymisationMethod.RandomiseDateTime;
                        break;
                    default:
                        throw new ArgumentException($"Invalid value for AnonymisationMethod found in Anonymisation config: {dicomTagConfig["AnonymisationMethod"]}. Permitted options are: \"Keep\", \"Hash\" and \"Random\"");
                }

                var fields = typeof(DicomTag).GetFields();
                foreach (var field in fields)
                {
                    if (string.Equals(field.Name, dicomTagConfig["DicomTagID"], StringComparison.Ordinal))
                    {
                        parsedDicomTags.Add(new DicomTagAnonymisation(
                          dicomTagID: (DicomTag)field.GetValue(null),
                          anonymisationProtocol: anonMethod)
                        );
                        break;
                    }
                }
                if (parsedDicomTags.Count == 0)
                {
                    throw new ArgumentException($"Unknown DicomTag found in config: {dicomTagConfig["DicomTagID"]}");
                }
            }

            return parsedDicomTags;
        }

        public IEnumerable<DicomTagAnonymisation> DicomTagsAnonymisationConfig => _dicomTagsAnonymisationConfig;

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
            hashCode = hashCode * -1521134295 + EqualityComparer<IEnumerable<DicomTagAnonymisation>>.Default.GetHashCode(DicomTagsAnonymisationConfig);
            return hashCode;
        }
    }
}
