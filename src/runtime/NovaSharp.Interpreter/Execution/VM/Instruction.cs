namespace NovaSharp.Interpreter.Execution.VM
{
    using System;
    using System.Collections.Generic;
    using System.Globalization;
    using System.IO;
    using System.Linq;
    using Debugging;
    using NovaSharp.Interpreter.DataTypes;

    /// <summary>
    /// Represents a single NovaSharp VM instruction with its operands and source reference.
    /// </summary>
    internal class Instruction
    {
        internal OpCode OpCode { get; set; }
        internal SymbolRef Symbol { get; set; }
        internal SymbolRef[] SymbolList { get; set; }
        internal string Name { get; set; }
        internal DynValue Value { get; set; }
        internal int NumVal { get; set; }
        internal int NumVal2 { get; set; }
        internal SourceRef SourceCodeRef { get; set; }

        /// <summary>
        /// Initializes an instruction bound to the specified source reference.
        /// </summary>
        internal Instruction(SourceRef sourceref)
        {
            SourceCodeRef = sourceref;
        }

        /// <summary>
        /// Returns a human-readable representation of the instruction for dumps/disassembly.
        /// </summary>
        public override string ToString()
        {
            string append = OpCode.ToString().ToUpperInvariant();

            int usage = (int)OpCode.GetFieldUsage();

            if (usage != 0)
            {
                append += GenSpaces();
            }

            if (
                (OpCode == OpCode.Meta)
                || (
                    (usage & ((int)InstructionFieldUsage.NumValAsCodeAddress))
                    == (int)InstructionFieldUsage.NumValAsCodeAddress
                )
            )
            {
                append += " " + NumVal.ToString("X8", CultureInfo.InvariantCulture);
            }
            else if ((usage & ((int)InstructionFieldUsage.NumVal)) != 0)
            {
                append += " " + NumVal.ToString(CultureInfo.InvariantCulture);
            }

            if ((usage & ((int)InstructionFieldUsage.NumVal2)) != 0)
            {
                append += " " + NumVal2.ToString(CultureInfo.InvariantCulture);
            }

            if ((usage & ((int)InstructionFieldUsage.Name)) != 0)
            {
                append += " " + Name;
            }

            if ((usage & ((int)InstructionFieldUsage.Value)) != 0)
            {
                append += " " + PurifyFromNewLines(Value);
            }

            if ((usage & ((int)InstructionFieldUsage.Symbol)) != 0)
            {
                append += " " + Symbol;
            }

            if (((usage & ((int)InstructionFieldUsage.SymbolList)) != 0) && (SymbolList != null))
            {
                string[] serializedSymbols = new string[SymbolList.Length];
                for (int i = 0; i < SymbolList.Length; i++)
                {
                    serializedSymbols[i] = SymbolList[i].ToString();
                }

                append += " " + string.Join(",", serializedSymbols);
            }

            return append;
        }

        private static string PurifyFromNewLines(DynValue value)
        {
            if (value == null)
            {
                return "";
            }

            return value.ToString().Replace('\n', ' ').Replace('\r', ' ');
        }

        private string GenSpaces()
        {
            return new string(' ', 10 - OpCode.ToString().Length);
        }

        /// <summary>
        /// Writes the instruction to the binary chunk format.
        /// </summary>
        /// <param name="wr">Destination writer.</param>
        /// <param name="baseAddress">Base instruction pointer used for relative jumps.</param>
        /// <param name="symbolMap">Map linking symbols to indices.</param>
        internal void WriteBinary(
            BinaryWriter wr,
            int baseAddress,
            Dictionary<SymbolRef, int> symbolMap
        )
        {
            wr.Write((byte)OpCode);

            int usage = (int)OpCode.GetFieldUsage();

            if (
                (usage & ((int)InstructionFieldUsage.NumValAsCodeAddress))
                == (int)InstructionFieldUsage.NumValAsCodeAddress
            )
            {
                wr.Write(NumVal - baseAddress);
            }
            else if ((usage & ((int)InstructionFieldUsage.NumVal)) != 0)
            {
                wr.Write(NumVal);
            }

            if ((usage & ((int)InstructionFieldUsage.NumVal2)) != 0)
            {
                wr.Write(NumVal2);
            }

            if ((usage & ((int)InstructionFieldUsage.Name)) != 0)
            {
                wr.Write(Name ?? "");
            }

            if ((usage & ((int)InstructionFieldUsage.Value)) != 0)
            {
                DumpValue(wr, Value);
            }

            if ((usage & ((int)InstructionFieldUsage.Symbol)) != 0)
            {
                WriteSymbol(wr, Symbol, symbolMap);
            }

            if ((usage & ((int)InstructionFieldUsage.SymbolList)) != 0)
            {
                wr.Write(SymbolList.Length);
                for (int i = 0; i < SymbolList.Length; i++)
                {
                    WriteSymbol(wr, SymbolList[i], symbolMap);
                }
            }
        }

        private static void WriteSymbol(
            BinaryWriter wr,
            SymbolRef symbolRef,
            Dictionary<SymbolRef, int> symbolMap
        )
        {
            int id = (symbolRef == null) ? -1 : symbolMap[symbolRef];
            wr.Write(id);
        }

        private static SymbolRef ReadSymbol(BinaryReader rd, SymbolRef[] deserializedSymbols)
        {
            int id = rd.ReadInt32();

            if (id < 0)
            {
                return null;
            }

            return deserializedSymbols[id];
        }

        /// <summary>
        /// Reads a binary chunk instruction and reconstructs metadata required by the VM.
        /// </summary>
        /// <param name="chunkRef">Source reference of the chunk.</param>
        /// <param name="rd">Binary reader.</param>
        /// <param name="baseAddress">Base instruction pointer.</param>
        /// <param name="envTable">Environment used for tables inside dumps.</param>
        /// <param name="deserializedSymbols">Previously deserialized symbol table.</param>
        /// <returns>The reconstructed instruction.</returns>
        internal static Instruction ReadBinary(
            SourceRef chunkRef,
            BinaryReader rd,
            int baseAddress,
            Table envTable,
            SymbolRef[] deserializedSymbols
        )
        {
            Instruction that = new(chunkRef) { OpCode = (OpCode)rd.ReadByte() };

            int usage = (int)that.OpCode.GetFieldUsage();

            if (
                (usage & ((int)InstructionFieldUsage.NumValAsCodeAddress))
                == (int)InstructionFieldUsage.NumValAsCodeAddress
            )
            {
                that.NumVal = rd.ReadInt32() + baseAddress;
            }
            else if ((usage & ((int)InstructionFieldUsage.NumVal)) != 0)
            {
                that.NumVal = rd.ReadInt32();
            }

            if ((usage & ((int)InstructionFieldUsage.NumVal2)) != 0)
            {
                that.NumVal2 = rd.ReadInt32();
            }

            if ((usage & ((int)InstructionFieldUsage.Name)) != 0)
            {
                that.Name = rd.ReadString();
            }

            if ((usage & ((int)InstructionFieldUsage.Value)) != 0)
            {
                that.Value = ReadValue(rd, envTable);
            }

            if ((usage & ((int)InstructionFieldUsage.Symbol)) != 0)
            {
                that.Symbol = ReadSymbol(rd, deserializedSymbols);
            }

            if ((usage & ((int)InstructionFieldUsage.SymbolList)) != 0)
            {
                int len = rd.ReadInt32();
                that.SymbolList = new SymbolRef[len];

                for (int i = 0; i < that.SymbolList.Length; i++)
                {
                    that.SymbolList[i] = ReadSymbol(rd, deserializedSymbols);
                }
            }

            return that;
        }

        private static DynValue ReadValue(BinaryReader rd, Table envTable)
        {
            bool isnull = !rd.ReadBoolean();

            if (isnull)
            {
                return null;
            }

            DataType dt = (DataType)rd.ReadByte();

            switch (dt)
            {
                case DataType.Nil:
                    return DynValue.NewNil();
                case DataType.Void:
                    return DynValue.Void;
                case DataType.Boolean:
                    return DynValue.NewBoolean(rd.ReadBoolean());
                case DataType.Number:
                    return DynValue.NewNumber(rd.ReadDouble());
                case DataType.String:
                    return DynValue.NewString(rd.ReadString());
                case DataType.Table:
                    return DynValue.NewTable(envTable);
                default:
                    throw new NotSupportedException($"Unsupported type in chunk dump : {dt}");
            }
        }

        private static void DumpValue(BinaryWriter wr, DynValue value)
        {
            if (value == null)
            {
                wr.Write(false);
                return;
            }

            wr.Write(true);
            wr.Write((byte)value.Type);

            switch (value.Type)
            {
                case DataType.Nil:
                case DataType.Void:
                case DataType.Table:
                    break;
                case DataType.Boolean:
                    wr.Write(value.Boolean);
                    break;
                case DataType.Number:
                    wr.Write(value.Number);
                    break;
                case DataType.String:
                    wr.Write(value.String);
                    break;
                default:
                    throw new NotSupportedException(
                        $"Unsupported type in chunk dump : {value.Type}"
                    );
            }
        }

        /// <summary>
        /// Extracts the symbol operands referenced by the instruction (if any).
        /// </summary>
        /// <param name="symbolList">List operand, if present.</param>
        /// <param name="symbol">Single symbol operand, if present.</param>
        internal void GetSymbolReferences(out SymbolRef[] symbolList, out SymbolRef symbol)
        {
            int usage = (int)OpCode.GetFieldUsage();

            symbol = null;
            symbolList = null;

            if ((usage & ((int)InstructionFieldUsage.Symbol)) != 0)
            {
                symbol = Symbol;
            }

            if ((usage & ((int)InstructionFieldUsage.SymbolList)) != 0)
            {
                symbolList = SymbolList;
            }
        }
    }
}
