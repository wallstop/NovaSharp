namespace WallstopStudios.NovaSharp.Interpreter.Tests.TUnit.Units.Execution
{
    using System;
    using System.Threading.Tasks;
    using global::TUnit.Assertions;
    using WallstopStudios.NovaSharp.Interpreter.Execution;
    using WallstopStudios.NovaSharp.Interpreter.Execution.VM;

    public sealed class InstructionFieldUsageExtensionsTUnitTests
    {
        [global::TUnit.Core.Test]
        [global::TUnit.Core.Arguments((int)OpCode.TblInitN, 0)]
        [global::TUnit.Core.Arguments((int)OpCode.Pop, (int)InstructionFieldUsage.NumVal)]
        [global::TUnit.Core.Arguments(
            (int)OpCode.Enter,
            (int)(
                InstructionFieldUsage.SymbolList
                | InstructionFieldUsage.NumVal
                | InstructionFieldUsage.NumVal2
            )
        )]
        [global::TUnit.Core.Arguments(
            (int)OpCode.Jump,
            (int)InstructionFieldUsagePresets.NumValAsCodeAddress
        )]
        [global::TUnit.Core.Arguments(
            (int)OpCode.Swap,
            (int)(InstructionFieldUsage.NumVal | InstructionFieldUsage.NumVal2)
        )]
        [global::TUnit.Core.Arguments(
            (int)OpCode.Clean,
            (int)(
                InstructionFieldUsage.SymbolList
                | InstructionFieldUsage.NumVal
                | InstructionFieldUsage.NumVal2
            )
        )]
        [global::TUnit.Core.Arguments((int)OpCode.Local, (int)InstructionFieldUsage.Symbol)]
        [global::TUnit.Core.Arguments(
            (int)OpCode.IndexSet,
            (int)(
                InstructionFieldUsage.Symbol
                | InstructionFieldUsage.Value
                | InstructionFieldUsage.NumVal
                | InstructionFieldUsage.NumVal2
                | InstructionFieldUsage.Name
            )
        )]
        [global::TUnit.Core.Arguments(
            (int)OpCode.StoreLcl,
            (int)(
                InstructionFieldUsage.Symbol
                | InstructionFieldUsage.NumVal
                | InstructionFieldUsage.NumVal2
            )
        )]
        [global::TUnit.Core.Arguments(
            (int)OpCode.Index,
            (int)(InstructionFieldUsage.Value | InstructionFieldUsage.Name)
        )]
        [global::TUnit.Core.Arguments((int)OpCode.Args, (int)InstructionFieldUsage.SymbolList)]
        [global::TUnit.Core.Arguments(
            (int)OpCode.BeginFn,
            (int)(
                InstructionFieldUsage.SymbolList
                | InstructionFieldUsage.NumVal
                | InstructionFieldUsage.NumVal2
            )
        )]
        [global::TUnit.Core.Arguments(
            (int)OpCode.Closure,
            (int)(
                InstructionFieldUsage.SymbolList | InstructionFieldUsagePresets.NumValAsCodeAddress
            )
        )]
        [global::TUnit.Core.Arguments((int)OpCode.Nop, (int)InstructionFieldUsage.Name)]
        [global::TUnit.Core.Arguments(
            (int)OpCode.Call,
            (int)(InstructionFieldUsage.NumVal | InstructionFieldUsage.Name)
        )]
        [global::TUnit.Core.Arguments(
            (int)OpCode.Meta,
            (int)(
                InstructionFieldUsage.NumVal
                | InstructionFieldUsage.NumVal2
                | InstructionFieldUsage.Value
                | InstructionFieldUsage.Name
            )
        )]
        public async Task GetFieldUsageReturnsExpectedFlags(int opCodeValue, int expectedFlags)
        {
            OpCode opCode = (OpCode)opCodeValue;
            InstructionFieldUsage expected = (InstructionFieldUsage)expectedFlags;
            InstructionFieldUsage usage = opCode.GetFieldUsage();

            await Assert.That(usage).IsEqualTo(expected).ConfigureAwait(false);
        }

        [global::TUnit.Core.Test]
        public void GetFieldUsageThrowsOnUnknownOpcode()
        {
            OpCode invalidOpcode = (OpCode)int.MaxValue;

            Assert.Throws<NotImplementedException>(() => invalidOpcode.GetFieldUsage());
        }
    }
}
