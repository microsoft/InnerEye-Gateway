// Copyright (c) Microsoft Corporation.
// Licensed under the MIT license.

namespace Microsoft.InnerEye.Azure.Segmentation.API.Common
{
    using System;
    using System.Collections.Generic;
    using System.IO;
    using System.IO.Compression;
    using Dicom;

    /// <summary>
    /// DICOM compression helpers.
    /// </summary>
    public static class DicomCompressionHelpers
    {
        /// <summary>
        /// The channel identifier and dicom series separator.
        /// </summary>
        public const char ChannelIdAndDicomSeriesSeparator = '/';

        /// <summary>
        /// The default compression level.
        /// </summary>
        public static readonly CompressionLevel DefaultCompressionLevel = CompressionLevel.Optimal;

        /// <summary>
        /// Zips and serializes dicom files.
        /// </summary>
        /// <param name="channels">The channels.</param>
        /// <param name="compressionLevel">The compression level.</param>
        /// <returns></returns>
        public static byte[] CompressDicomFiles(
            IEnumerable<ChannelData> channels,
            CompressionLevel compressionLevel)
        {
            channels = channels ?? throw new ArgumentNullException(nameof(channels));

            using (var memoryStream = new MemoryStream())
            {
                using (var archive = new ZipArchive(memoryStream, ZipArchiveMode.Create))
                {
                    var fileIndex = 0;

                    foreach (var channel in channels)
                    {
                        foreach (var file in channel.DicomFiles)
                        {
                            var archiveEntry = archive.CreateEntry(
                                $"{channel.ChannelID}{ChannelIdAndDicomSeriesSeparator}{fileIndex++}",
                                compressionLevel);

                            SaveDicomToArchive(file, archiveEntry);
                        }
                    }
                }

                return memoryStream.ToArray();
            }
        }

        /// <summary>
        /// Saves the dicom file to a zip archive entry.
        /// </summary>
        /// <param name="dicomFile">The dicom file.</param>
        /// <param name="zipArchiveEntry">The zip archive entry.</param>
        private static void SaveDicomToArchive(DicomFile dicomFile, ZipArchiveEntry zipArchiveEntry)
        {
            using (var entryStream = zipArchiveEntry.Open())
            {
                dicomFile.Save(entryStream);
            }
        }
    }
}
