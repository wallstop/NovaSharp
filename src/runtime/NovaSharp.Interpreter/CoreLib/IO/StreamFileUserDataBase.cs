namespace NovaSharp.Interpreter.CoreLib.IO
{
    using System;
    using System.IO;
    using System.Text;
    using NovaSharp.Interpreter.DataTypes;
    using NovaSharp.Interpreter.Errors;
    using NovaSharp.Interpreter.Interop.Attributes;

    /// <summary>
    /// Abstract class implementing a file Lua userdata. Methods are meant to be called by Lua code.
    /// </summary>
    internal abstract class StreamFileUserDataBase : FileUserDataBase
    {
        protected Stream _stream;
        protected StreamReader _reader;
        protected StreamWriter _writer;
        protected bool _closed;
        protected long _logicalPosition;

        protected void Initialize(Stream stream, StreamReader reader, StreamWriter writer)
        {
            _stream = stream;
            _reader = reader;
            _writer = writer;
            _logicalPosition = _stream != null && _stream.CanSeek ? _stream.Position : 0;
        }

        private void CheckFileIsNotClosed()
        {
            if (_closed)
            {
                throw new ScriptRuntimeException("attempt to use a closed file");
            }
        }

        protected override bool Eof()
        {
            CheckFileIsNotClosed();

            if (_reader != null)
            {
                if (_stream.CanSeek)
                {
                    if (!_reader.EndOfStream)
                    {
                        return false;
                    }

                    return _logicalPosition >= _stream.Length;
                }

                return _reader.EndOfStream;
            }
            else
            {
                return false;
            }
        }

        protected override string ReadLine()
        {
            CheckFileIsNotClosed();

            StringBuilder line = new();
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

        protected override string ReadToEnd()
        {
            CheckFileIsNotClosed();

            StringBuilder remainder = new();

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

        protected override string ReadBuffer(int p)
        {
            CheckFileIsNotClosed();
            long positionBeforeRead = _stream.CanSeek ? _logicalPosition : 0;
            char[] buffer = new char[p];
            int length = _reader.ReadBlock(buffer, 0, p);
            if (_stream.CanSeek && _reader != null && length > 0)
            {
                Encoding encoding = _reader.CurrentEncoding;
                long bytesRead = encoding.GetByteCount(buffer, 0, length);
                long expectedPosition = positionBeforeRead + bytesRead;
                _logicalPosition = expectedPosition;
                ResetReaderBuffer(expectedPosition);
            }

            return new string(buffer, 0, length);
        }

        protected override char Peek()
        {
            CheckFileIsNotClosed();
            return (char)_reader.Peek();
        }

        protected override void Write(string value)
        {
            CheckFileIsNotClosed();
            _writer.Write(value);
        }

        protected override int PeekRaw()
        {
            CheckFileIsNotClosed();
            return _reader?.Peek() ?? -1;
        }

        protected override string Close()
        {
            CheckFileIsNotClosed();

            if (_writer != null)
            {
                _writer.Dispose();
            }

            if (_reader != null)
            {
                _reader.Dispose();
            }

            _stream.Dispose();

            _closed = true;

            return null;
        }

        public override bool Flush()
        {
            CheckFileIsNotClosed();

            try
            {
                if (_writer != null)
                {
                    _writer.Flush();
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

        public override long Seek(string whence, long offset = 0)
        {
            CheckFileIsNotClosed();
            try
            {
                if (whence != null)
                {
                    if (whence == "set")
                    {
                        _stream.Seek(offset, SeekOrigin.Begin);
                    }
                    else if (whence == "cur")
                    {
                        _stream.Seek(offset, SeekOrigin.Current);
                    }
                    else if (whence == "end")
                    {
                        _stream.Seek(offset, SeekOrigin.End);
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

                long position = _stream.Position;
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

        public override bool Setvbuf(string mode)
        {
            CheckFileIsNotClosed();
            try
            {
                if (_writer != null)
                {
                    _writer.AutoFlush = mode == "no" || mode == "line";
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

        protected override bool SupportsRewind
        {
            get { return _stream != null && _stream.CanSeek; }
        }

        protected override long GetCurrentPosition()
        {
            return _logicalPosition;
        }

        protected override void ResetToPosition(long position)
        {
            _logicalPosition = position;
            ResetReaderBuffer(position);
        }

        protected internal override bool IsOpen()
        {
            return !_closed;
        }

        protected void ResetReaderBuffer(long targetPosition)
        {
            if (_reader == null || !_stream.CanSeek)
            {
                return;
            }

            long currentPosition = _stream.Position;
            if (currentPosition == targetPosition)
            {
                return;
            }

            _reader.DiscardBufferedData();
            _stream.Seek(targetPosition, SeekOrigin.Begin);
            _logicalPosition = targetPosition;
        }
    }
}
