namespace WallstopStudios.NovaSharp.Interpreter.Execution
{
    using System;
    using Cysharp.Text;
    using WallstopStudios.NovaSharp.Interpreter.Execution.VM;

    /// <summary>
    /// Describes which fields of a bytecode instruction carry meaningful data for debugging/serialization.
    /// </summary>
    [Flags]
    internal enum InstructionFieldUsage
    {
        [Obsolete("Prefer explicit InstructionFieldUsage flags.", false)]
        None = 0,
        Symbol = 1 << 0,
        SymbolList = 1 << 1,
        Name = 1 << 2,
        Value = 1 << 3,
        NumVal = 1 << 4,
        NumVal2 = 1 << 5,
        NumValAsCodeAddressBit = 1 << 6,
    }

    /// <summary>
    /// Combined flag values for <see cref="InstructionFieldUsage"/>.
    /// </summary>
    internal static class InstructionFieldUsagePresets
    {
        /// <summary>
        /// Indicates the NumVal field represents a code address (jump target).
        /// </summary>
        public const InstructionFieldUsage NumValAsCodeAddress =
            InstructionFieldUsage.NumValAsCodeAddressBit | InstructionFieldUsage.NumVal;
    }

    /// <summary>
    /// Helpers for classifying opcodes based on their field usage patterns.
    /// </summary>
    internal static class InstructionFieldUsageExtensions
    {
        /// <summary>
        /// Returns the <see cref="InstructionFieldUsage"/> flags describing which instruction fields are relevant for the opcode.
        /// </summary>
        internal static InstructionFieldUsage GetFieldUsage(this OpCode op)
        {
            switch (op)
            {
                case OpCode.TblInitN:
                case OpCode.Scalar:
                case OpCode.IterUpd:
                case OpCode.IterPrep:
                case OpCode.NewTable:
                case OpCode.Concat:
                case OpCode.LessEq:
                case OpCode.Less:
                case OpCode.Eq:
                case OpCode.Add:
                case OpCode.Sub:
                case OpCode.Mul:
                case OpCode.Div:
                case OpCode.Mod:
                case OpCode.Not:
                case OpCode.Len:
                case OpCode.Neg:
                case OpCode.Power:
                case OpCode.CNot:
                case OpCode.ToBool:
                    return default;
                case OpCode.Pop:
                case OpCode.Copy:
                case OpCode.TblInitI:
                case OpCode.ExpTuple:
                case OpCode.Incr:
                case OpCode.ToNum:
                case OpCode.Ret:
                case OpCode.MkTuple:
                    return InstructionFieldUsage.NumVal;
                case OpCode.Enter:
                case OpCode.Leave:
                case OpCode.Exit:
                    return InstructionFieldUsage.SymbolList
                        | InstructionFieldUsage.NumVal
                        | InstructionFieldUsage.NumVal2;
                case OpCode.Jump:
                case OpCode.Jf:
                case OpCode.JNil:
                case OpCode.JFor:
                case OpCode.JtOrPop:
                case OpCode.JfOrPop:
                    return InstructionFieldUsagePresets.NumValAsCodeAddress;
                case OpCode.Swap:
                    return InstructionFieldUsage.NumVal | InstructionFieldUsage.NumVal2;
                case OpCode.Clean:
                    return InstructionFieldUsage.SymbolList
                        | InstructionFieldUsage.NumVal
                        | InstructionFieldUsage.NumVal2;
                case OpCode.Local:
                case OpCode.UpValue:
                    return InstructionFieldUsage.Symbol;
                case OpCode.IndexSet:
                case OpCode.IndexSetN:
                case OpCode.IndexSetL:
                    return InstructionFieldUsage.Symbol
                        | InstructionFieldUsage.Value
                        | InstructionFieldUsage.NumVal
                        | InstructionFieldUsage.NumVal2
                        | InstructionFieldUsage.Name;
                case OpCode.StoreLcl:
                case OpCode.StoreUpv:
                    return InstructionFieldUsage.Symbol
                        | InstructionFieldUsage.NumVal
                        | InstructionFieldUsage.NumVal2;
                case OpCode.Index:
                case OpCode.IndexL:
                case OpCode.IndexN:
                case OpCode.Literal:
                    return InstructionFieldUsage.Value | InstructionFieldUsage.Name;
                case OpCode.Args:
                    return InstructionFieldUsage.SymbolList;
                case OpCode.BeginFn:
                    return InstructionFieldUsage.SymbolList
                        | InstructionFieldUsage.NumVal
                        | InstructionFieldUsage.NumVal2;
                case OpCode.Closure:
                    return InstructionFieldUsage.SymbolList
                        | InstructionFieldUsagePresets.NumValAsCodeAddress;
                case OpCode.Nop:
                case OpCode.Debug:
                case OpCode.Invalid:
                    return InstructionFieldUsage.Name;
                case OpCode.Call:
                case OpCode.ThisCall:
                    return InstructionFieldUsage.NumVal | InstructionFieldUsage.Name;
                case OpCode.Meta:
                    return InstructionFieldUsage.NumVal
                        | InstructionFieldUsage.NumVal2
                        | InstructionFieldUsage.Value
                        | InstructionFieldUsage.Name;
                default:
                    throw new NotImplementedException(
                        ZString.Concat("InstructionFieldUsage for instruction ", (int)op)
                    );
            }
        }
    }
}
