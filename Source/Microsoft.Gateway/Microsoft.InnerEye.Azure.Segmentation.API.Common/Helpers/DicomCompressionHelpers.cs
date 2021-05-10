namespace Microsoft.InnerEye.Azure.Segmentation.API.Common
{
    using System;
    using System.Collections.Generic;
    using System.Diagnostics.CodeAnalysis;
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
        /// Decompresses a collection of ZIP archive entry streams from the compressed stream.
        /// </summary>
        /// <param name="compressedData">The compressed data stream.</param>
        /// <returns>The decompressed array of streams.</returns>
        /// <exception cref="ArgumentNullException">If the compressed data stream is null.</exception>
        public static IReadOnlyList<Stream> DecompressStreams(Stream compressedData)
        {
            compressedData = compressedData ?? throw new ArgumentNullException(nameof(compressedData));

            using (var zipArchive = new ZipArchive(compressedData, ZipArchiveMode.Read, leaveOpen: true))
            {
                var result = new List<Stream>(zipArchive.Entries.Count);
                for (var i = 0; i < zipArchive.Entries.Count; i++)
                {
                    result.Add(CreateStream(zipArchive.Entries[i]));
                }

                return result;
            }
        }

        /// <summary>
        /// Disposes of a collection of streams.
        /// </summary>
        /// <param name="streams">The streams collection.</param>
        /// <exception cref="ArgumentNullException">The collection of streams is null.</exception>
        public static void Dispose(this IReadOnlyList<Stream> streams)
        {
            streams = streams ?? throw new ArgumentNullException(nameof(streams));

            foreach (var stream in streams)
            {
                stream.Dispose();
            }
        }

        /// <summary>
        /// Opens the ZIP archive entry, copies the stream to the provided file stream, and returns a new DICOM file instance.
        /// Note: The FO-DICOM DicomFile will not read tags from the stream >64KB. Therefore, to read a tag (such as pixel data)
        /// the dicomFileStream must be left open.
        /// </summary>
        /// <param name="zipArchiveEntry">The ZIP archive entry.</param>
        /// <param name="dicomFileStream">The DICOM file stream.</param>
        /// <returns>The DICOM file.</returns>
        /// <exception cref="ArgumentNullException">If the zip archive entry or DICOM file stream is null.</exception>
        public static DicomFile OpenDicomFile(this ZipArchiveEntry zipArchiveEntry, Stream dicomFileStream)
        {
            zipArchiveEntry = zipArchiveEntry ?? throw new ArgumentNullException(nameof(zipArchiveEntry));
            dicomFileStream = dicomFileStream ?? throw new ArgumentNullException(nameof(dicomFileStream));

            using (var entryStream = zipArchiveEntry.Open())
            {
                entryStream.CopyTo(dicomFileStream);
            }

            dicomFileStream.Seek(0, SeekOrigin.Begin);
            return DicomFile.Open(dicomFileStream);
        }

        /// <summary>
        /// Creates a new memory stream from a ZIP archive entry.
        /// </summary>
        /// <param name="zipArchiveEntry">The ZIP archive entry.</param>
        /// <returns>The new memory stream from the ZIP archive entry.</returns>
        /// <exception cref="ArgumentNullException">If the zip archive entry is null.</exception>
        [SuppressMessage("Microsoft.Reliability", "CA2000:Dispose objects before losing scope", Justification = "Resulting stream is returned to caller to dispose.")]
        public static Stream CreateStream(this ZipArchiveEntry zipArchiveEntry)
        {
            zipArchiveEntry = zipArchiveEntry ?? throw new ArgumentNullException(nameof(zipArchiveEntry));

            var memoryStream = new MemoryStream();

            try
            {
                using (var entryStream = zipArchiveEntry.Open())
                {
                    entryStream.CopyTo(memoryStream);
                }

                memoryStream.Seek(0, SeekOrigin.Begin);

                var result = memoryStream;
                memoryStream = null;
                return result;
            }
            finally
            {
                memoryStream?.Dispose();
            }
        }

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
        /// Compresses the dicom files to a stream.
        /// </summary>
        /// <param name="stream">The stream to compress the DICOM files to.</param>
        /// <param name="dicomFiles">The dicom files.</param>
        public static void CompressDicomFilesToStream(Stream stream, params DicomFile[] dicomFiles)
        {
            CompressDicomFilesToStream(stream, (IEnumerable<DicomFile>)dicomFiles);
        }

        /// <summary>
        /// Compresses the dicom files to a stream.
        /// </summary>
        /// <param name="stream">The stream to compress the DICOM files to.</param>
        /// <param name="dicomFiles">The dicom files.</param>
        public static void CompressDicomFilesToStream(Stream stream, IEnumerable<DicomFile> dicomFiles)
        {
            dicomFiles = dicomFiles ?? throw new ArgumentNullException(nameof(dicomFiles));

            // Note: we must leave the stream open here as we do not own the stream.
            CompressDicomFilesToStream(dicomFiles, stream, DefaultCompressionLevel, leaveStreamOpen: true);
        }

        /// <summary>
        /// Compresses the dicom files to a zip archive and returns a byte array.
        /// </summary>
        /// <param name="dicomFiles">The dicom files.</param>
        /// <returns>The compressed Dicom files.</returns>
        public static byte[] CompressDicomFiles(IEnumerable<DicomFile> dicomFiles)
        {
            return CompressDicomFiles(DefaultCompressionLevel, dicomFiles);
        }

        /// <summary>
        /// Compresses the dicom files to a zip archive and returns a byte array.
        /// </summary>
        /// <param name="dicomFiles">The dicom files.</param>
        /// <returns>The compressed Dicom files.</returns>
        public static byte[] CompressDicomFiles(params DicomFile[] dicomFiles)
        {
            return CompressDicomFiles(DefaultCompressionLevel, dicomFiles);
        }

        /// <summary>
        /// Compresses the dicom files to a zip archive and returns a byte array.
        /// </summary>
        /// <param name="compressionLevel">The compression level.</param>
        /// <param name="dicomFiles">The dicom files.</param>
        /// <returns>The compressed Dicom files.</returns>
        public static byte[] CompressDicomFiles(CompressionLevel compressionLevel, params DicomFile[] dicomFiles)
        {
            return CompressDicomFiles(compressionLevel, (IEnumerable<DicomFile>)dicomFiles);
        }

        /// <summary>
        /// Compresses the dicom files to a zip archive and returns a byte array.
        /// </summary>
        /// <param name="compressionLevel">The compression level.</param>
        /// <param name="dicomFiles">The dicom files.</param>
        /// <returns>The compressed Dicom files.</returns>
        public static byte[] CompressDicomFiles(CompressionLevel compressionLevel, IEnumerable<DicomFile> dicomFiles)
        {
            dicomFiles = dicomFiles ?? throw new ArgumentNullException(nameof(dicomFiles));

            using (var memoryStream = new MemoryStream())
            {
                CompressDicomFilesToStream(dicomFiles, memoryStream, compressionLevel, leaveStreamOpen: false);
                return memoryStream.ToArray();
            }
        }

        /// <summary>
        /// The input byte[] containing zip file contents is deflated.
        /// Resulting file names and their contents are returned.
        /// </summary>
        public static IEnumerable<(string FileName, byte[] Data)> DecompressPayload(byte[] data)
        {
            data = data ?? throw new ArgumentNullException(nameof(data));

            using (var memoryStream = new MemoryStream(data))
            {
                return DecompressPayload(memoryStream);
            }
        }

        /// <summary>
        /// The input stream containing zip file contents is deflated.
        /// Resulting file names and their contents are returned.
        /// </summary>
        public static IEnumerable<(string FileName, byte[] Data)> DecompressPayload(Stream inputStream)
        {
            inputStream = inputStream ?? throw new ArgumentNullException(nameof(inputStream));

            using (var archive = new ZipArchive(inputStream, ZipArchiveMode.Read))
            {
                foreach (var archiveEntry in archive.Entries)
                {
                    yield return (archiveEntry.FullName, ToByteArray(archiveEntry));
                }
            }
        }

        /// <summary>
        /// Compresses the DICOM files to a stream.
        /// </summary>
        /// <param name="stream">The stream.</param>
        /// <param name="compressionLevel">The compression level.</param>
        /// <param name="leaveStreamOpen">If the stream should be left open.</param>
        /// <param name="dicomFiles">The dicom files.</param>
        private static void CompressDicomFilesToStream(
            IEnumerable<DicomFile> dicomFiles,
            Stream stream,
            CompressionLevel compressionLevel,
            bool leaveStreamOpen)
        {
            using (var archive = new ZipArchive(stream, ZipArchiveMode.Create, leaveStreamOpen))
            {
                var fileIndex = 0;

                foreach (var file in dicomFiles)
                {
                    SaveDicomToArchive(file, archive.CreateEntry($"{fileIndex++}", compressionLevel));
                }
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

        /// <summary>
        /// Read the contents of ziparchiveentry and return them as a byte array.
        /// </summary>
        /// <param name="entry">The zip archive entry.</param>
        /// <returns>The entry contents as a byte array.</returns>
        private static byte[] ToByteArray(ZipArchiveEntry entry)
        {
            using (var entryStream = entry.Open())
            {
                using (var memoryStream = new MemoryStream())
                {
                    entryStream.CopyTo(memoryStream);
                    return memoryStream.ToArray();
                }
            }
        }
    }
}