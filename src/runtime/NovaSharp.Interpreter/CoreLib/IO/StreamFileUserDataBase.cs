namespace NovaSharp.Interpreter.CoreLib.IO
{
    using System.IO;
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
        protected bool _closed = false;

        protected void Initialize(Stream stream, StreamReader reader, StreamWriter writer)
        {
            _stream = stream;
            _reader = reader;
            _writer = writer;
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
            return _reader.ReadLine();
        }

        protected override string ReadToEnd()
        {
            CheckFileIsNotClosed();
            return _reader.ReadToEnd();
        }

        protected override string ReadBuffer(int p)
        {
            CheckFileIsNotClosed();
            char[] buffer = new char[p];
            int length = _reader.ReadBlock(buffer, 0, p);
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

            if (_writer != null)
            {
                _writer.Flush();
            }

            return true;
        }

        public override long Seek(string whence, long offset = 0)
        {
            CheckFileIsNotClosed();
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

            return _stream.Position;
        }

        public override bool Setvbuf(string mode)
        {
            CheckFileIsNotClosed();
            if (_writer != null)
            {
                _writer.AutoFlush = mode == "no" || mode == "line";
            }

            return true;
        }

        protected internal override bool IsOpen()
        {
            return !_closed;
        }
    }
}
