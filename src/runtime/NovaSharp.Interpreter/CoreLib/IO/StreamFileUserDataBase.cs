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
        protected Stream StreamInstance;
        protected StreamReader StreamReaderInstance;
        protected StreamWriter StreamWriterInstance;
        protected bool IsClosed;
        protected long LogicalPosition;

        protected void Initialize(Stream stream, StreamReader reader, StreamWriter writer)
        {
            StreamInstance = stream;
            StreamReaderInstance = reader;
            StreamWriterInstance = writer;
            LogicalPosition =
                StreamInstance != null && StreamInstance.CanSeek ? StreamInstance.Position : 0;
        }

        private void CheckFileIsNotClosed()
        {
            if (IsClosed)
            {
                throw new ScriptRuntimeException("attempt to use a closed file");
            }
        }

        protected override bool Eof()
        {
            CheckFileIsNotClosed();

            if (StreamReaderInstance != null)
            {
                if (StreamInstance.CanSeek)
                {
                    if (!StreamReaderInstance.EndOfStream)
                    {
                        return false;
                    }

                    return LogicalPosition >= StreamInstance.Length;
                }

                return StreamReaderInstance.EndOfStream;
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
            long positionBeforeRead = StreamInstance.CanSeek ? LogicalPosition : 0;
            char[] buffer = new char[p];
            int length = StreamReaderInstance.ReadBlock(buffer, 0, p);
            if (StreamInstance.CanSeek && StreamReaderInstance != null && length > 0)
            {
                Encoding encoding = StreamReaderInstance.CurrentEncoding;
                long bytesRead = encoding.GetByteCount(buffer, 0, length);
                long expectedPosition = positionBeforeRead + bytesRead;
                LogicalPosition = expectedPosition;
                ResetReaderBuffer(expectedPosition);
            }

            return new string(buffer, 0, length);
        }

        protected override char Peek()
        {
            CheckFileIsNotClosed();
            return (char)StreamReaderInstance.Peek();
        }

        protected override void Write(string value)
        {
            CheckFileIsNotClosed();
            StreamWriterInstance.Write(value);
        }

        protected override int PeekRaw()
        {
            CheckFileIsNotClosed();
            return StreamReaderInstance?.Peek() ?? -1;
        }

        protected override string Close()
        {
            CheckFileIsNotClosed();

            if (StreamWriterInstance != null)
            {
                StreamWriterInstance.Dispose();
            }

            if (StreamReaderInstance != null)
            {
                StreamReaderInstance.Dispose();
            }

            StreamInstance.Dispose();

            IsClosed = true;

            return null;
        }

        public override bool Flush()
        {
            CheckFileIsNotClosed();

            try
            {
                if (StreamWriterInstance != null)
                {
                    StreamWriterInstance.Flush();
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
                        StreamInstance.Seek(offset, SeekOrigin.Begin);
                    }
                    else if (whence == "cur")
                    {
                        StreamInstance.Seek(offset, SeekOrigin.Current);
                    }
                    else if (whence == "end")
                    {
                        StreamInstance.Seek(offset, SeekOrigin.End);
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

                long position = StreamInstance.Position;
                LogicalPosition = position;
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
                if (StreamWriterInstance != null)
                {
                    StreamWriterInstance.AutoFlush = mode == "no" || mode == "line";
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
            get { return StreamInstance != null && StreamInstance.CanSeek; }
        }

        protected override long GetCurrentPosition()
        {
            return LogicalPosition;
        }

        protected override void ResetToPosition(long position)
        {
            LogicalPosition = position;
            ResetReaderBuffer(position);
        }

        protected internal override bool IsOpen()
        {
            return !IsClosed;
        }

        protected void ResetReaderBuffer(long targetPosition)
        {
            if (StreamReaderInstance == null || !StreamInstance.CanSeek)
            {
                return;
            }

            long currentPosition = StreamInstance.Position;
            if (currentPosition == targetPosition)
            {
                return;
            }

            StreamReaderInstance.DiscardBufferedData();
            StreamInstance.Seek(targetPosition, SeekOrigin.Begin);
            LogicalPosition = targetPosition;
        }
    }
}
