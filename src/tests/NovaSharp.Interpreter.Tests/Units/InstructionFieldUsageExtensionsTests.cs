namespace NovaSharp.Interpreter.Tests.Units
{
    using System;
    using NovaSharp.Interpreter.Execution;
    using NovaSharp.Interpreter.Execution.VM;
    using NUnit.Framework;

    [TestFixture]
    public sealed class InstructionFieldUsageExtensionsTests
    {
        [TestCase((int)OpCode.TblInitN, 0)]
        [TestCase((int)OpCode.Pop, (int)InstructionFieldUsage.NumVal)]
        [TestCase(
            (int)OpCode.Enter,
            (int)(
                InstructionFieldUsage.SymbolList
                | InstructionFieldUsage.NumVal
                | InstructionFieldUsage.NumVal2
            )
        )]
        [TestCase((int)OpCode.Jump, (int)InstructionFieldUsage.NumValAsCodeAddress)]
        [TestCase(
            (int)OpCode.Swap,
            (int)(InstructionFieldUsage.NumVal | InstructionFieldUsage.NumVal2)
        )]
        [TestCase(
            (int)OpCode.Clean,
            (int)(
                InstructionFieldUsage.SymbolList
                | InstructionFieldUsage.NumVal
                | InstructionFieldUsage.NumVal2
            )
        )]
        [TestCase((int)OpCode.Local, (int)InstructionFieldUsage.Symbol)]
        [TestCase(
            (int)OpCode.IndexSet,
            (int)(
                InstructionFieldUsage.Symbol
                | InstructionFieldUsage.Value
                | InstructionFieldUsage.NumVal
                | InstructionFieldUsage.NumVal2
            )
        )]
        [TestCase(
            (int)OpCode.StoreLcl,
            (int)(
                InstructionFieldUsage.Symbol
                | InstructionFieldUsage.NumVal
                | InstructionFieldUsage.NumVal2
            )
        )]
        [TestCase((int)OpCode.Index, (int)InstructionFieldUsage.Value)]
        [TestCase((int)OpCode.Args, (int)InstructionFieldUsage.SymbolList)]
        [TestCase(
            (int)OpCode.BeginFn,
            (int)(
                InstructionFieldUsage.SymbolList
                | InstructionFieldUsage.NumVal
                | InstructionFieldUsage.NumVal2
            )
        )]
        [TestCase(
            (int)OpCode.Closure,
            (int)(InstructionFieldUsage.SymbolList | InstructionFieldUsage.NumValAsCodeAddress)
        )]
        [TestCase((int)OpCode.Nop, (int)InstructionFieldUsage.Name)]
        [TestCase(
            (int)OpCode.Call,
            (int)(InstructionFieldUsage.NumVal | InstructionFieldUsage.Name)
        )]
        [TestCase(
            (int)OpCode.Meta,
            (int)(
                InstructionFieldUsage.NumVal
                | InstructionFieldUsage.NumVal2
                | InstructionFieldUsage.Value
                | InstructionFieldUsage.Name
            )
        )]
        public void GetFieldUsageReturnsExpectedFlags(int opCodeValue, int expectedFlags)
        {
            OpCode opCode = (OpCode)opCodeValue;
            InstructionFieldUsage expected = (InstructionFieldUsage)expectedFlags;
            InstructionFieldUsage usage = opCode.GetFieldUsage();

            Assert.That(usage, Is.EqualTo(expected));
        }

        [Test]
        public void GetFieldUsageThrowsOnUnknownOpcode()
        {
            OpCode invalidOpcode = (OpCode)int.MaxValue;

            Assert.That(
                () => invalidOpcode.GetFieldUsage(),
                Throws
                    .TypeOf<NotImplementedException>()
                    .With.Message.Contains("InstructionFieldUsage")
            );
        }
    }
}
