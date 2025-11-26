namespace NovaSharp.Interpreter.IO
{
    using System;
    using System.Diagnostics.CodeAnalysis;
    using System.IO;

    /// <summary>
    /// An adapter over Stream which bypasses the Dispose and Close methods.
    /// Used to work around the pesky wrappers .NET has over Stream (BinaryReader, StreamWriter, etc.) which think they
    /// own the Stream and close them when they shouldn't. Damn.
    /// </summary>
    [SuppressMessage(
        "Usage",
        "CA2213:Disposable fields should be disposed",
        Justification = "Wrapper intentionally leaves the underlying stream open so nested readers/writers cannot close shared streams."
    )]
    public class UndisposableStream : Stream
    {
        private readonly Stream _stream;

        /// <summary>
        /// Wraps the provided stream and suppresses disposal semantics.
        /// </summary>
        public UndisposableStream(Stream stream)
        {
            _stream = stream;
        }

        protected override void Dispose(bool disposing)
        {
            base.Dispose(disposing);
        }

#if !(PCL || ENABLE_DOTNET || NETFX_CORE)
        /// <summary>
        /// Suppresses closing the wrapped stream.
        /// </summary>
        public override void Close() { }
#endif

        /// <inheritdoc />
        public override bool CanRead
        {
            get { return _stream.CanRead; }
        }

        /// <inheritdoc />
        public override bool CanSeek
        {
            get { return _stream.CanSeek; }
        }

        /// <inheritdoc />
        public override bool CanWrite
        {
            get { return _stream.CanWrite; }
        }

        /// <inheritdoc />
        public override void Flush()
        {
            _stream.Flush();
        }

        /// <inheritdoc />
        public override long Length
        {
            get { return _stream.Length; }
        }

        /// <inheritdoc />
        public override long Position
        {
            get { return _stream.Position; }
            set { _stream.Position = value; }
        }

        /// <inheritdoc />
        public override int Read(byte[] buffer, int offset, int count)
        {
            return _stream.Read(buffer, offset, count);
        }

        /// <inheritdoc />
        public override long Seek(long offset, SeekOrigin origin)
        {
            return _stream.Seek(offset, origin);
        }

        /// <inheritdoc />
        public override void SetLength(long value)
        {
            _stream.SetLength(value);
        }

        /// <inheritdoc />
        public override void Write(byte[] buffer, int offset, int count)
        {
            _stream.Write(buffer, offset, count);
        }

#if (!(NETFX_CORE))
        /// <inheritdoc />
        public override IAsyncResult BeginRead(
            byte[] buffer,
            int offset,
            int count,
            AsyncCallback callback,
            object state
        )
        {
            return _stream.BeginRead(buffer, offset, count, callback, state);
        }

        /// <inheritdoc />
        public override IAsyncResult BeginWrite(
            byte[] buffer,
            int offset,
            int count,
            AsyncCallback callback,
            object state
        )
        {
            return _stream.BeginWrite(buffer, offset, count, callback, state);
        }

        /// <inheritdoc />
        public override void EndWrite(IAsyncResult asyncResult)
        {
            _stream.EndWrite(asyncResult);
        }

        /// <inheritdoc />
        public override int EndRead(IAsyncResult asyncResult)
        {
            return _stream.EndRead(asyncResult);
        }
#endif

        /// <inheritdoc />
        public override bool CanTimeout
        {
            get { return _stream.CanTimeout; }
        }

        /// <inheritdoc />
        public override bool Equals(object obj)
        {
            return _stream.Equals(obj);
        }

        /// <inheritdoc />
        public override int GetHashCode()
        {
            return _stream.GetHashCode();
        }

        /// <inheritdoc />
        public override int ReadByte()
        {
            return _stream.ReadByte();
        }

        /// <inheritdoc />
        public override int ReadTimeout
        {
            get { return _stream.ReadTimeout; }
            set { _stream.ReadTimeout = value; }
        }

        /// <inheritdoc />
        public override string ToString()
        {
            return _stream.ToString();
        }

        /// <inheritdoc />
        public override void WriteByte(byte value)
        {
            _stream.WriteByte(value);
        }

        /// <inheritdoc />
        public override int WriteTimeout
        {
            get { return _stream.WriteTimeout; }
            set { _stream.WriteTimeout = value; }
        }
    }
}
