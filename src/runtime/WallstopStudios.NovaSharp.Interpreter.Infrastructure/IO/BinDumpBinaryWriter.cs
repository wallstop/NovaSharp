namespace WallstopStudios.NovaSharp.Interpreter.Infrastructure.IO
{
    using System.Collections.Generic;
    using System.IO;
    using System.Text;

    /// <summary>
    /// "Optimized" BinaryWriter which shares strings and use a dumb compression for integers
    /// </summary>
    public class BinDumpBinaryWriter : BinaryWriter
    {
        private readonly Dictionary<string, int> _stringMap = new();

        /// <summary>
        /// Initializes a writer that uses the default encoding.
        /// </summary>
        public BinDumpBinaryWriter(Stream s)
            : base(s) { }

        /// <summary>
        /// Initializes a writer using the specified encoding.
        /// </summary>
        public BinDumpBinaryWriter(Stream s, Encoding e)
            : base(s, e) { }

        /// <summary>
        /// Writes a compressed unsigned integer (1, 2, or 4 bytes) reserving 0x7E/0x7F as sentinel markers.
        /// </summary>
        public override void Write(uint value)
        {
            byte v8 = (byte)value;

            if ((uint)v8 == value && (v8 != 0x7F) && (v8 != 0x7E))
            {
                base.Write(v8);
            }
            else
            {
                ushort v16 = (ushort)value;

                if ((uint)v16 == value)
                {
                    base.Write((byte)0x7F);
                    base.Write(v16);
                }
                else
                {
                    base.Write((byte)0x7E);
                    base.Write(value);
                }
            }
        }

        /// <summary>
        /// Writes a compressed signed integer (1, 2, or 4 bytes) reserving 0x7E/0x7F as sentinel markers.
        /// </summary>
        public override void Write(int value)
        {
            sbyte vsbyte = (sbyte)value;

            if ((int)vsbyte == value && (vsbyte != 0x7F) && (vsbyte != 0x7E))
            {
                base.Write(vsbyte);
            }
            else
            {
                short vshort = (short)value;

                if ((int)vshort == value)
                {
                    base.Write((sbyte)0x7F);
                    base.Write(vshort);
                }
                else
                {
                    base.Write((sbyte)0x7E);
                    base.Write(value);
                }
            }
        }

        /// <summary>
        /// Writes a string using a shared string table (deduplicates repeated values).
        /// </summary>
        public override void Write(string value)
        {
            if (_stringMap.TryGetValue(value, out int pos))
            {
                Write(_stringMap[value]);
            }
            else
            {
                pos = _stringMap.Count;
                _stringMap[value] = pos;

                Write(pos);
                base.Write(value);
            }
        }
    }
}
