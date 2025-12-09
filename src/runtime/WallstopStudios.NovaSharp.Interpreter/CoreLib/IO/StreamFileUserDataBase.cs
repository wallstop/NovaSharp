namespace WallstopStudios.NovaSharp.Interpreter.CoreLib.IO
{
    using System;
    using System.IO;
    using System.Text;
    using Cysharp.Text;
    using WallstopStudios.NovaSharp.Interpreter.DataStructs;
    using WallstopStudios.NovaSharp.Interpreter.Errors;

    /// <summary>
    /// Abstract class implementing a file Lua userdata. Methods are meant to be called by Lua code.
    /// </summary>
    internal abstract class StreamFileUserDataBase : FileUserDataBase
    {
        /// <summary>Underlying stream instance backing this userdata.</summary>
        protected Stream _streamInstance;

        /// <summary>Reader used when the file is opened for input.</summary>
        protected StreamReader _streamReaderInstance;

        /// <summary>Writer used when the file is opened for output.</summary>
        protected StreamWriter _streamWriterInstance;

        /// <summary>True when the userdata has been closed.</summary>
        protected bool _isClosed;

        /// <summary>Logical position tracked for buffered readers.</summary>
        protected long _logicalPosition;

        /// <summary>Whether the file was opened in binary mode.</summary>
        protected bool _isBinaryMode;

        /// <summary>Whether there's a pending carriage return from the previous read.</summary>
        protected bool _pendingCarriageReturnOnRead;

        /// <summary>
        /// Initializes the userdata with the supplied stream, reader, and writer handles.
        /// </summary>
        protected void Initialize(
            Stream stream,
            StreamReader reader,
            StreamWriter writer,
            bool isBinaryMode
        )
        {
            _streamInstance = stream;
            _streamReaderInstance = reader;
            _streamWriterInstance = writer;
            _logicalPosition =
                _streamInstance != null && _streamInstance.CanSeek ? _streamInstance.Position : 0;
            _isBinaryMode = isBinaryMode;
            _pendingCarriageReturnOnRead = false;
        }

        /// <summary>Exposes the backing stream to derived types for inspection.</summary>
        protected Stream StreamInstance
        {
            get => _streamInstance;
            set => _streamInstance = value;
        }

        /// <summary>Exposes the backing reader to derived types for inspection.</summary>
        protected StreamReader StreamReaderInstance
        {
            get => _streamReaderInstance;
            set => _streamReaderInstance = value;
        }

        /// <summary>Exposes the backing writer to derived types for inspection.</summary>
        protected StreamWriter StreamWriterInstance
        {
            get => _streamWriterInstance;
            set => _streamWriterInstance = value;
        }

        private void CheckFileIsNotClosed()
        {
            if (_isClosed)
            {
                throw new ScriptRuntimeException("attempt to use a closed file");
            }
        }

        /// <inheritdoc />
        protected override bool Eof()
        {
            CheckFileIsNotClosed();

            if (_streamReaderInstance != null)
            {
                if (_streamInstance.CanSeek)
                {
                    if (!_streamReaderInstance.EndOfStream)
                    {
                        return false;
                    }

                    return _logicalPosition >= _streamInstance.Length;
                }

                return _streamReaderInstance.EndOfStream;
            }
            else
            {
                return false;
            }
        }

        /// <inheritdoc />
        protected override string ReadLine()
        {
            CheckFileIsNotClosed();

            using Utf16ValueStringBuilder line = ZStringBuilder.Create();
            bool readAny = false;
            bool reachedEof = false;

            while (true)
            {
                int peekValue = PeekRaw();
                if (peekValue == -1)
                {
                    reachedEof = true;
                    break;
                }

                char current = (char)peekValue;

                if (current == '\n')
                {
                    ReadBuffer(1);
                    break;
                }

                if (current == '\r')
                {
                    ReadBuffer(1);
                    if (PeekRaw() == '\n')
                    {
                        ReadBuffer(1);
                    }
                    break;
                }

                string chunk = ReadBuffer(1);
                if (chunk.Length == 0)
                {
                    break;
                }

                line.Append(chunk);
                readAny = true;
            }

            if (!readAny && line.Length == 0 && reachedEof)
            {
                return null;
            }

            return line.ToString();
        }

        /// <inheritdoc />
        protected override string ReadToEnd()
        {
            CheckFileIsNotClosed();

            using Utf16ValueStringBuilder remainder = ZStringBuilder.Create();

            while (true)
            {
                string chunk = ReadBuffer(4096);
                if (chunk.Length == 0)
                {
                    break;
                }

                remainder.Append(chunk);
            }

            return remainder.ToString();
        }

        /// <inheritdoc />
        protected override string ReadBuffer(int p)
        {
            CheckFileIsNotClosed();
            long positionBeforeRead = _streamInstance.CanSeek ? _logicalPosition : 0;
            char[] buffer = new char[p];
            int length = _streamReaderInstance.ReadBlock(buffer, 0, p);
            if (_streamInstance.CanSeek && _streamReaderInstance != null && length > 0)
            {
                Encoding encoding = _streamReaderInstance.CurrentEncoding;
                long bytesRead = encoding.GetByteCount(buffer, 0, length);
                long expectedPosition = positionBeforeRead + bytesRead;
                _logicalPosition = expectedPosition;
                ResetReaderBuffer(expectedPosition);
            }

            string chunk = new string(buffer, 0, length);
            return NormalizeReadChunk(chunk);
        }

        /// <inheritdoc />
        protected override char Peek()
        {
            CheckFileIsNotClosed();
            return (char)_streamReaderInstance.Peek();
        }

        /// <inheritdoc />
        protected override void Write(string value)
        {
            CheckFileIsNotClosed();
            string textToWrite = value;

            bool containsLineFeed = false;

            if (value != null)
            {
                for (int i = 0; i < value.Length; i++)
                {
                    if (value[i] == '\n')
                    {
                        containsLineFeed = true;
                        break;
                    }
                }
            }

            if (!_isBinaryMode && value != null && Environment.NewLine != "\n" && containsLineFeed)
            {
                textToWrite = value.Replace("\n", Environment.NewLine, StringComparison.Ordinal);
            }

            _streamWriterInstance.Write(textToWrite);
        }

        /// <inheritdoc />
        protected override int PeekRaw()
        {
            CheckFileIsNotClosed();
            return _streamReaderInstance?.Peek() ?? -1;
        }

        /// <inheritdoc />
        protected override string Close()
        {
            CheckFileIsNotClosed();

            if (_streamWriterInstance != null)
            {
                _streamWriterInstance.Dispose();
            }

            if (_streamReaderInstance != null)
            {
                _streamReaderInstance.Dispose();
            }

            _streamInstance.Dispose();

            _isClosed = true;
            _pendingCarriageReturnOnRead = false;

            return null;
        }

        /// <inheritdoc />
        public override bool Flush()
        {
            CheckFileIsNotClosed();

            try
            {
                if (_streamWriterInstance != null)
                {
                    _streamWriterInstance.Flush();
                }
            }
            catch (ScriptRuntimeException)
            {
                throw;
            }
            catch (Exception ex)
            {
                throw new ScriptRuntimeException(ex.Message);
            }

            return true;
        }

        /// <inheritdoc />
        public override long Seek(string whence, long offset = 0)
        {
            CheckFileIsNotClosed();
            try
            {
                if (whence != null)
                {
                    if (whence == "set")
                    {
                        _streamInstance.Seek(offset, SeekOrigin.Begin);
                    }
                    else if (whence == "cur")
                    {
                        _streamInstance.Seek(offset, SeekOrigin.Current);
                    }
                    else if (whence == "end")
                    {
                        _streamInstance.Seek(offset, SeekOrigin.End);
                    }
                    else
                    {
                        throw ScriptRuntimeException.BadArgument(
                            0,
                            "seek",
                            "invalid option '" + whence + "'"
                        );
                    }
                }

                long position = _streamInstance.Position;
                _logicalPosition = position;
                ResetReaderBuffer(position);
                return position;
            }
            catch (ScriptRuntimeException)
            {
                throw;
            }
            catch (Exception ex)
            {
                throw new ScriptRuntimeException(ex.Message);
            }
        }

        /// <inheritdoc />
        public override bool Setvbuf(string mode)
        {
            CheckFileIsNotClosed();
            try
            {
                if (_streamWriterInstance != null)
                {
                    _streamWriterInstance.AutoFlush = mode == "no" || mode == "line";
                }
            }
            catch (ScriptRuntimeException)
            {
                throw;
            }
            catch (Exception ex)
            {
                throw new ScriptRuntimeException(ex.Message);
            }

            return true;
        }

        /// <inheritdoc />
        protected override bool SupportsRewind
        {
            get { return _streamInstance != null && _streamInstance.CanSeek; }
        }

        /// <inheritdoc />
        protected override long GetCurrentPosition()
        {
            return _logicalPosition;
        }

        /// <inheritdoc />
        protected override void ResetToPosition(long position)
        {
            _logicalPosition = position;
            ResetReaderBuffer(position);
        }

        /// <inheritdoc />
        protected internal override bool IsOpen()
        {
            return !_isClosed;
        }

        protected void ResetReaderBuffer(long targetPosition)
        {
            if (_streamReaderInstance == null || !_streamInstance.CanSeek)
            {
                return;
            }

            long currentPosition = _streamInstance.Position;
            if (currentPosition == targetPosition)
            {
                return;
            }

            _streamReaderInstance.DiscardBufferedData();
            _streamInstance.Seek(targetPosition, SeekOrigin.Begin);
            _logicalPosition = targetPosition;
            _pendingCarriageReturnOnRead = false;
        }

        /// <summary>
        /// Normalizes CRLF sequences in a read chunk to LF when not in binary mode.
        /// </summary>
        /// <param name="chunk">The raw chunk read from the stream.</param>
        /// <returns>The normalized chunk with CRLF converted to LF on Windows.</returns>
        internal string NormalizeReadChunk(string chunk)
        {
            if (_isBinaryMode)
            {
                return chunk;
            }

            if (chunk.Length == 0)
            {
                if (_pendingCarriageReturnOnRead && UsesWindowsNewLine())
                {
                    _pendingCarriageReturnOnRead = false;
                    return "\r";
                }

                return chunk;
            }

            if (!UsesWindowsNewLine())
            {
                if (_pendingCarriageReturnOnRead)
                {
                    _pendingCarriageReturnOnRead = false;
                    return "\r" + chunk;
                }

                return chunk;
            }

            bool needsBuilder = false;
            int copyStart = 0;

            if (_pendingCarriageReturnOnRead)
            {
                needsBuilder = true;
            }
            else
            {
                // Scan to see if we need normalization at all
                for (int i = 0; i < chunk.Length; i++)
                {
                    if (chunk[i] == '\r')
                    {
                        needsBuilder = true;
                        break;
                    }
                }
            }

            if (!needsBuilder)
            {
                return chunk;
            }

            using Utf16ValueStringBuilder builder = ZStringBuilder.Create();

            if (_pendingCarriageReturnOnRead)
            {
                if (chunk[0] == '\n')
                {
                    builder.Append('\n');
                    copyStart = 1;
                }
                else
                {
                    builder.Append('\r');
                }

                _pendingCarriageReturnOnRead = false;
            }

            for (int i = copyStart; i < chunk.Length; i++)
            {
                char current = chunk[i];

                if (current == '\r')
                {
                    if (i + 1 < chunk.Length)
                    {
                        if (chunk[i + 1] == '\n')
                        {
                            if (copyStart < i)
                            {
                                builder.Append(chunk.AsSpan(copyStart, i - copyStart));
                            }

                            builder.Append('\n');
                            copyStart = i + 2;
                            i++;
                            continue;
                        }
                    }
                    else
                    {
                        if (copyStart < i)
                        {
                            builder.Append(chunk.AsSpan(copyStart, i - copyStart));
                        }

                        _pendingCarriageReturnOnRead = true;
                        copyStart = chunk.Length;
                        break;
                    }
                }
            }

            if (copyStart < chunk.Length)
            {
                builder.Append(chunk.AsSpan(copyStart, chunk.Length - copyStart));
            }

            return builder.ToString();
        }

        private static bool UsesWindowsNewLine()
        {
            return Environment.NewLine == "\r\n";
        }
    }
}
