using System;
using System.Diagnostics;
using System.Text;
using System.IO;

namespace Microsoft.InnerEye.Listener.Tests.DataProviderTests
{
    /// <summary>
    /// A TextWriter wrapper for Debug interface of Visual Studio so that we could output fo-dicom log events there.
    /// </summary>
    public class DebugTextWriter : StreamWriter
    {
        public DebugTextWriter()
            : base(new DebugOutStream(), Encoding.Unicode, 1024)
        {
            this.AutoFlush = true;
        }

        private sealed class DebugOutStream : Stream
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
            public override long Position
            {
                get => throw bad_op;
                set => throw bad_op;
            }

            private static InvalidOperationException bad_op => new InvalidOperationException();
        };
    }
}
