namespace NovaSharp.Interpreter.CoreLib.IO
{
    using System.IO;

    /// <summary>
    /// Abstract class implementing a file Lua userdata. Methods are meant to be called by Lua code.
    /// </summary>
    internal abstract class StreamFileUserDataBase : FileUserDataBase
    {
        protected Stream _Stream;
        protected StreamReader _Reader;
        protected StreamWriter _Writer;
        protected bool _Closed = false;

        protected void Initialize(Stream stream, StreamReader reader, StreamWriter writer)
        {
            _Stream = stream;
            _Reader = reader;
            _Writer = writer;
        }

        private void CheckFileIsNotClosed()
        {
            if (_Closed)
            {
                throw new ScriptRuntimeException("attempt to use a closed file");
            }
        }

        protected override bool Eof()
        {
            CheckFileIsNotClosed();

            if (_Reader != null)
            {
                return _Reader.EndOfStream;
            }
            else
            {
                return false;
            }
        }

        protected override string ReadLine()
        {
            CheckFileIsNotClosed();
            return _Reader.ReadLine();
        }

        protected override string ReadToEnd()
        {
            CheckFileIsNotClosed();
            return _Reader.ReadToEnd();
        }

        protected override string ReadBuffer(int p)
        {
            CheckFileIsNotClosed();
            char[] buffer = new char[p];
            int length = _Reader.ReadBlock(buffer, 0, p);
            return new string(buffer, 0, length);
        }

        protected override char Peek()
        {
            CheckFileIsNotClosed();
            return (char)_Reader.Peek();
        }

        protected override void Write(string value)
        {
            CheckFileIsNotClosed();
            _Writer.Write(value);
        }

        protected override string Close()
        {
            CheckFileIsNotClosed();

            if (_Writer != null)
            {
                _Writer.Dispose();
            }

            if (_Reader != null)
            {
                _Reader.Dispose();
            }

            _Stream.Dispose();

            _Closed = true;

            return null;
        }

        public override bool Flush()
        {
            CheckFileIsNotClosed();

            if (_Writer != null)
            {
                _Writer.Flush();
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
                    _Stream.Seek(offset, SeekOrigin.Begin);
                }
                else if (whence == "cur")
                {
                    _Stream.Seek(offset, SeekOrigin.Current);
                }
                else if (whence == "end")
                {
                    _Stream.Seek(offset, SeekOrigin.End);
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

            return _Stream.Position;
        }

        public override bool Setvbuf(string mode)
        {
            CheckFileIsNotClosed();
            if (_Writer != null)
            {
                _Writer.AutoFlush = (mode == "no" || mode == "line");
            }

            return true;
        }

        protected internal override bool Isopen()
        {
            return !_Closed;
        }
    }
}
