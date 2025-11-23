namespace NovaSharp.Interpreter.Execution.VM
{
    using System;
    using System.Collections.Generic;
    using System.IO;
    using System.Linq;
    using System.Text;
    using Debugging;
    using IO;
    using NovaSharp.Interpreter.DataTypes;

    /// <content>
    /// Provides chunk dump/undump helpers for persisting bytecode.
    /// </content>
    internal sealed partial class Processor
    {
        private const ulong DumpChunkMagic = 0x1A0D234E4F4F4D1D;
        private const int DumpChunkVersion = 0x150;

        /// <summary>
        /// Determines whether the provided stream contains a NovaSharp chunk header.
        /// </summary>
        /// <param name="stream">Stream to inspect (position is restored).</param>
        /// <returns><c>true</c> if the header matches; otherwise, <c>false</c>.</returns>
        internal static bool IsDumpStream(Stream stream)
        {
            if (stream.Length >= 8)
            {
                using BinaryReader br = new(stream, Encoding.UTF8);
                ulong magic = br.ReadUInt64();
                stream.Seek(-8, SeekOrigin.Current);
                return magic == DumpChunkMagic;
            }
            return false;
        }

        /// <summary>
        /// Writes the bytecode for the current chunk to the provided stream.
        /// </summary>
        /// <param name="stream">Destination stream.</param>
        /// <param name="baseAddress">Instruction pointer to start dumping from.</param>
        /// <param name="hasUpvalues">Whether the function being dumped captures upvalues.</param>
        /// <returns>The instruction pointer immediately after the dumped chunk.</returns>
        /// <exception cref="ArgumentException">Thrown when <paramref name="baseAddress"/> does not point to a Meta instruction.</exception>
        internal int Dump(Stream stream, int baseAddress, bool hasUpvalues)
        {
            using BinDumpBinaryWriter bw = new(stream, Encoding.UTF8);
            Dictionary<SymbolRef, int> symbolMap = new();

            Instruction meta = FindMeta(ref baseAddress);

            if (meta == null)
            {
                throw new ArgumentException("Invalid base address for dump.", nameof(baseAddress));
            }

            bw.Write(DumpChunkMagic);
            bw.Write(DumpChunkVersion);
            bw.Write(hasUpvalues);
            bw.Write(meta.NumVal);

            for (int i = 0; i <= meta.NumVal; i++)
            {
                _rootChunk
                    .Code[baseAddress + i]
                    .GetSymbolReferences(out SymbolRef[] symbolList, out SymbolRef symbol);

                if (symbol != null)
                {
                    AddSymbolToMap(symbolMap, symbol);
                }

                if (symbolList != null)
                {
                    foreach (SymbolRef s in symbolList)
                    {
                        AddSymbolToMap(symbolMap, s);
                    }
                }
            }

            foreach (SymbolRef sr in symbolMap.Keys.ToArray())
            {
                if (sr.EnvironmentRef != null)
                {
                    AddSymbolToMap(symbolMap, sr.EnvironmentRef);
                }
            }

            SymbolRef[] allSymbols = new SymbolRef[symbolMap.Count];

            foreach (KeyValuePair<SymbolRef, int> pair in symbolMap)
            {
                allSymbols[pair.Value] = pair.Key;
            }

            bw.Write(symbolMap.Count);

            foreach (SymbolRef sym in allSymbols)
            {
                sym.WriteBinary(bw);
            }

            foreach (SymbolRef sym in allSymbols)
            {
                sym.WriteBinaryEnv(bw, symbolMap);
            }

            for (int i = 0; i <= meta.NumVal; i++)
            {
                _rootChunk.Code[baseAddress + i].WriteBinary(bw, baseAddress, symbolMap);
            }

            return meta.NumVal + baseAddress + 1;
        }

        /// <summary>
        /// Adds the specified symbol to the serialization map when not already present.
        /// </summary>
        private static void AddSymbolToMap(Dictionary<SymbolRef, int> symbolMap, SymbolRef s)
        {
            if (!symbolMap.ContainsKey(s))
            {
                symbolMap.Add(s, symbolMap.Count);
            }
        }

        /// <summary>
        /// Reads a chunk from the provided stream and appends it to the processor's bytecode buffer.
        /// </summary>
        /// <param name="stream">Source stream.</param>
        /// <param name="sourceId">Identifier for the source, used in <see cref="SourceRef"/>.</param>
        /// <param name="envTable">Environment table used to recreate table literals.</param>
        /// <param name="hasUpvalues">Outputs whether the chunk declares upvalues.</param>
        /// <returns>The base address where the chunk was inserted.</returns>
        /// <exception cref="ArgumentException">Thrown when the header is invalid or the version mismatches.</exception>
        internal int Undump(Stream stream, int sourceId, Table envTable, out bool hasUpvalues)
        {
            int baseAddress = _rootChunk.Code.Count;
            SourceRef sourceRef = new(sourceId, 0, 0, 0, 0, false);

            using BinDumpBinaryReader br = new(stream, Encoding.UTF8);
            ulong headerMark = br.ReadUInt64();

            if (headerMark != DumpChunkMagic)
            {
                throw new ArgumentException("Not a NovaSharp chunk");
            }

            int version = br.ReadInt32();

            if (version != DumpChunkVersion)
            {
                throw new ArgumentException("Invalid version");
            }

            hasUpvalues = br.ReadBoolean();

            int len = br.ReadInt32();

            int numSymbs = br.ReadInt32();
            SymbolRef[] allSymbs = new SymbolRef[numSymbs];

            for (int i = 0; i < numSymbs; i++)
            {
                allSymbs[i] = SymbolRef.ReadBinary(br);
            }

            for (int i = 0; i < numSymbs; i++)
            {
                allSymbs[i].ReadBinaryEnv(br, allSymbs);
            }

            for (int i = 0; i <= len; i++)
            {
                Instruction instruction = Instruction.ReadBinary(
                    sourceRef,
                    br,
                    baseAddress,
                    envTable,
                    allSymbs
                );
                _rootChunk.Code.Add(instruction);
            }

            return baseAddress;
        }
    }
}
