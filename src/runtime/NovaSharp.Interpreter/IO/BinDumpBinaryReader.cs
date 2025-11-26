namespace NovaSharp.Interpreter.IO
{
    using System.Collections.Generic;
    using System.IO;
    using System.Text;

    /// <summary>
    /// "Optimized" BinaryReader which shares strings and use a dumb compression for integers
    /// </summary>
    public class BinDumpBinaryReader : BinaryReader
    {
        /// <summary>
        /// Initializes a reader that uses the default encoding.
        /// </summary>
        public BinDumpBinaryReader(Stream s)
            : base(s) { }

        /// <summary>
        /// Initializes a reader using the specified encoding.
        /// </summary>
        public BinDumpBinaryReader(Stream s, Encoding e)
            : base(s, e) { }

        private readonly List<string> _strings = new();

        /// <summary>
        /// Reads an int32 using the compressed encoding (1, 2, or 4 bytes).
        /// </summary>
        public override int ReadInt32()
        {
            sbyte b = base.ReadSByte();

            if (b == 0x7F)
            {
                return (int)base.ReadInt16();
            }
            else if (b == 0x7E)
            {
                return (int)base.ReadInt32();
            }
            else
            {
                return (int)b;
            }
        }

        /// <summary>
        /// Reads an unsigned int32 using the compressed encoding (1, 2, or 4 bytes).
        /// </summary>
        public override uint ReadUInt32()
        {
            byte b = base.ReadByte();

            if (b == 0x7F)
            {
                return (uint)base.ReadUInt16();
            }
            else if (b == 0x7E)
            {
                return (uint)base.ReadUInt32();
            }
            else
            {
                return (uint)b;
            }
        }

        /// <summary>
        /// Reads a string using the shared string table (deduplicates repeated values).
        /// </summary>
        public override string ReadString()
        {
            int pos = ReadInt32();

            if (pos < _strings.Count)
            {
                return _strings[pos];
            }
            else if (pos == _strings.Count)
            {
                string str = base.ReadString();
                _strings.Add(str);
                return str;
            }
            else
            {
                throw new IOException("string map failure");
            }
        }
    }
}
