// Copyright (c) Microsoft Corporation.
// Licensed under the MIT license.

namespace Microsoft.InnerEye.Azure.Segmentation.Client
{
    using System;
    using System.Collections.Generic;
    using System.Linq;

    using Dicom;

    using Microsoft.InnerEye.Azure.Segmentation.API.Common;

    /// <summary>
    /// Class for replacing Dicom tags.
    /// </summary>
    public static class TagReplacer
    {
        /// <summary>
        /// Applies user DICOM tag replacement rules to a dataset
        /// </summary>
        /// <param name="dicomDataSet">The dicom dataset.</param>
        /// <param name="replacement">The replacement.</param>
        public static void ApplyUserReplacement(DicomDataset dicomDataSet, TagReplacement replacement)
        {
            dicomDataSet = dicomDataSet ?? throw new ArgumentNullException(nameof(dicomDataSet));
            replacement = replacement ?? throw new ArgumentNullException(nameof(replacement));

            // We must do contains otherwise if the tag is present but empty it will return null
            var tag = replacement.DicomTagIndex.DicomTag;
            if (dicomDataSet.Contains(tag))
            {
                var sourceTagValue = dicomDataSet.GetSingleValueOrDefault(tag, string.Empty);
                if (replacement.Operation == TagReplacementOperation.UpdateIfExists)
                {
                    dicomDataSet.AddOrUpdate(tag, replacement.Value);
                }
                else if (replacement.Operation == TagReplacementOperation.AppendIfExists)
                {
                    dicomDataSet.AddOrUpdate(tag, $"{sourceTagValue}{replacement.Value}");
                }
                else
                {
                    throw new InvalidOperationException(nameof(replacement));
                }
            }
        }

        /// <summary>
        /// Replaces the user tag.
        /// </summary>
        /// <param name="dicomDatasetDeAnonymized">The dicom dataset de anonymized.</param>
        /// <param name="tagReplacement">The tag replacement.</param>
        public static void ReplaceUserTag(
                    DicomDataset dicomDatasetDeAnonymized,
                    TagReplacement tagReplacement)
        {
            ApplyUserReplacement(dicomDatasetDeAnonymized, tagReplacement);

            // Handle sequences recursive
            foreach (var item in dicomDatasetDeAnonymized)
            {
                if (item is DicomSequence dicomSequence)
                {
                    foreach (var dataset in dicomSequence.Items)
                    {
                        ReplaceUserTag(dataset, tagReplacement);
                    }
                }
            }
        }

        /// <summary>
        /// Add or updates a Dicom tag at the top level of Dicom tags.
        /// </summary>
        /// <param name="dicomDatasetDeAnonymized">The dicom dataset de anonymized.</param>
        /// <param name="referenceDicomDataset">The reference dicom dataset.</param>
        /// <param name="addOrUpdateTag">The tag to add or update.</param>
        public static void SimpleTopLevelAddOrUpdate(DicomDataset dicomDatasetDeAnonymized, DicomDataset referenceDicomDataset, DicomTag addOrUpdateTag)
        {
            referenceDicomDataset = referenceDicomDataset ?? throw new ArgumentNullException(nameof(referenceDicomDataset));
            dicomDatasetDeAnonymized = dicomDatasetDeAnonymized ?? throw new ArgumentNullException(nameof(dicomDatasetDeAnonymized));

            if (referenceDicomDataset.Contains(addOrUpdateTag))
            {
                referenceDicomDataset.CopyTo(dicomDatasetDeAnonymized, addOrUpdateTag);
            }
        }

        /// <summary>
        /// Replaces the hashed values in the anonymised Dicom file.
        /// </summary>
        /// <param name="dicomFile">The anonymised Dicom file.</param>
        /// <param name="referenceDicomFiles">The reference dicom files and its associated anonymized representation.</param>
        /// <param name="hashedDicomTags">The dicom tags that have been hashed.</param>
        public static void ReplaceHashedValues(
            DicomFile dicomFile,
            IEnumerable<(DicomFile Original, DicomFile Anonymized)> referenceDicomFiles,
            IEnumerable<DicomTag> hashedDicomTags)
        {
            dicomFile = dicomFile ?? throw new ArgumentNullException(nameof(dicomFile));

            var hashDictionary = CreateHashDictionary(
                referenceDicomFiles.Select(x => (x.Original.Dataset, x.Anonymized.Dataset)),
                hashedDicomTags);

            ReplaceHashedValues(dicomFile.Dataset, hashDictionary);
        }

        /// <summary>
        /// Replaces the hashed values in the anonymised Dicom dataset.
        /// </summary>
        /// <param name="dicomDataset">The dicom dataset.</param>
        /// <param name="referenceDicomDatasets">The reference dicom datasets.</param>
        /// <param name="hashedDicomTags">The hashed dicom tags.</param>
        public static void ReplaceHashedValues(
            DicomDataset dicomDataset,
            IEnumerable<(DicomDataset Original, DicomDataset Anonymized)> referenceDicomDatasets,
            IEnumerable<DicomTag> hashedDicomTags)
        {
            var hashDictionary = CreateHashDictionary(referenceDicomDatasets, hashedDicomTags);

            ReplaceHashedValues(dicomDataset, hashDictionary);
        }

        /// <summary>
        /// Replaces the hashed values in the Dicom dataset with its original value.
        /// </summary>
        /// <param name="dicomDataset">The dicom dataset.</param>
        /// <param name="hashDictionary">The hash dictionary of the hash value as the key and the original value as a value.</param>
        /// <exception cref="ArgumentNullException">
        /// dicomDataset
        /// or
        /// hashDictionary
        /// </exception>
        private static void ReplaceHashedValues(
            DicomDataset dicomDataset,
            Dictionary<string, string> hashDictionary)
        {
            if (dicomDataset == null)
            {
                throw new ArgumentNullException(nameof(dicomDataset));
            }

            if (hashDictionary == null)
            {
                throw new ArgumentNullException(nameof(hashDictionary));
            }

            foreach (var dicomItem in dicomDataset.ToList())
            {
                if (dicomItem is DicomSequence dicomSequence)
                {
                    foreach (var dicomItemDataset in dicomSequence)
                    {
                        ReplaceHashedValues(dicomItemDataset, hashDictionary);
                    }
                }
                else
                {
                    var hashedValue = dicomDataset.GetSingleValueOrDefault(dicomItem.Tag, string.Empty);

                    if (hashDictionary.ContainsKey(hashedValue))
                    {
                        dicomDataset.AddOrUpdate(dicomItem.Tag, hashDictionary[hashedValue]);
                    }
                }
            }
        }

        /// <summary>
        /// Creates a dictionary of a hashed value and its original value.
        /// </summary>
        /// <param name="referenceDicomDatasets">The reference dicom datasets.</param>
        /// <param name="hashedDicomTags">The hashed dicom tags.</param>
        /// <returns>The hash dictionary.</returns>
        /// <exception cref="ArgumentException">If two values produce the same hash.</exception>
        /// <exception cref="ArgumentNullException">If any dicom dataset is null or any of the input parameters are null.</exception>
        private static Dictionary<string, string> CreateHashDictionary(
            IEnumerable<(DicomDataset Original, DicomDataset Anonymized)> referenceDicomDatasets,
            IEnumerable<DicomTag> hashedDicomTags)
        {
            if (referenceDicomDatasets == null)
            {
                throw new ArgumentNullException(nameof(referenceDicomDatasets));
            }

            if (hashedDicomTags == null)
            {
                throw new ArgumentNullException(nameof(hashedDicomTags));
            }

            var result = new Dictionary<string, string>();

            foreach (var (original, anonymised) in referenceDicomDatasets)
            {
                if (original == null)
                {
                    throw new ArgumentNullException(nameof(referenceDicomDatasets), "null field: Original");
                }

                if (anonymised == null)
                {
                    throw new ArgumentNullException(nameof(referenceDicomDatasets), "null field: Anonymised");
                }

                foreach (var dicomTag in hashedDicomTags)
                {
                    if (original.Contains(dicomTag) && anonymised.Contains(dicomTag))
                    {
                        var hashedValue = anonymised.GetSingleValueOrDefault(dicomTag, string.Empty);
                        var originalValue = original.GetSingleValueOrDefault(dicomTag, string.Empty);

                        if (!result.ContainsKey(hashedValue))
                        {
                            result[hashedValue] = originalValue;
                        }
                        else if (result[hashedValue] != originalValue)
                        {
                            // This should never happen
                            throw new ArgumentException($"We have two different values with the same hash. This is not good. Hashed Value: {hashedValue}, Value 1: {result[hashedValue]}, Value 2: {originalValue}");
                        }
                    }
                }
            }

            return result;
        }
    }
}