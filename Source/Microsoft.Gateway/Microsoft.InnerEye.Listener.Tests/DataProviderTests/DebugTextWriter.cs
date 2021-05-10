namespace Microsoft.InnerEye.Listener.Tests.DataProviderTests
{
    using System;
    using System.Diagnostics;
    using System.IO;
    using System.Text;

    /// <summary>
    /// A TextWriter wrapper for Debug interface of Visual Studio so that we could output fo-dicom log events there.
    /// </summary>
    public class DebugTextWriter : StreamWriter
    {
        /// <summary>
        /// Initialize a new instance of the <see cref="DebugTextWriter"/> class.
        /// </summary>
        /// <param name="stream">Output stream.</param>
        public DebugTextWriter(Stream stream)
            : base(stream, Encoding.Unicode, 1024)
        {
            AutoFlush = true;
        }
    }

    /// <summary>
    /// A stream that can only write to Debug.Write.
    /// </summary>
    public sealed class DebugOutStream : Stream
    {
        public override void Write(byte[] buffer, int offset, int count)
        {
            Debug.Write(Encoding.Unicode.GetString(buffer, offset, count));
        }

        public override bool CanRead => false;
        public override bool CanSeek => false;
        public override bool CanWrite => true;
        public override void Flush() => Debug.Flush();

        public override long Length => throw bad_op;
        public override int Read(byte[] buffer, int offset, int count) => throw bad_op;
        public override long Seek(long offset, SeekOrigin origin) => throw bad_op;
        public override void SetLength(long value) => throw bad_op;
        public override long Position {
            get => throw bad_op;
            set => throw bad_op;
        }

        private static InvalidOperationException bad_op => new InvalidOperationException();
    }
}
