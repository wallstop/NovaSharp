namespace WallstopStudios.NovaSharp.Interpreter.Tests.TUnit.Units.Execution.ProcessorExecution
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Threading.Tasks;
    using global::TUnit.Assertions;
    using WallstopStudios.NovaSharp.Interpreter;
    using WallstopStudios.NovaSharp.Interpreter.DataTypes;
    using WallstopStudios.NovaSharp.Interpreter.Debugging;
    using WallstopStudios.NovaSharp.Interpreter.Errors;
    using WallstopStudios.NovaSharp.Interpreter.Execution.Scopes;
    using WallstopStudios.NovaSharp.Interpreter.Execution.VM;

    public sealed class ProcessorResourceManagementTUnitTests
    {
        [global::TUnit.Core.Test]
        public async Task CloseSymbolsSubsetClosesValuesAndClearsTracking()
        {
            Script script = new();
            Processor processor = script.GetMainProcessorForTests();
            processor.ClearCallStackForTests();

            SymbolRef symbol = SymbolRef.Local("resource", 0, SymbolRefAttributes.ToBeClosed);
            bool closed = false;
            DynValue closable = CreateClosableValue(script, _ => closed = true);

            CallStackItem frame = new()
            {
                LocalScope = new[] { closable },
                BlocksToClose = new List<List<SymbolRef>> { new List<SymbolRef> { symbol } },
                ToBeClosedIndices = new HashSet<int> { 0 },
            };
            processor.PushCallStackFrameForTests(frame);

            processor.CloseSymbolsSubsetForTests(
                frame,
                new[] { symbol },
                DynValue.NewString("err")
            );

            await Assert.That(closed).IsTrue();
            await Assert.That(frame.LocalScope[0].IsNil()).IsTrue();
            bool containsIndex =
                frame.ToBeClosedIndices != null && frame.ToBeClosedIndices.Contains(0);
            await Assert.That(containsIndex).IsFalse();
            bool blocksCleared =
                frame.BlocksToClose == null
                || frame.BlocksToClose.All(list => list == null || list.Count == 0);
            await Assert.That(blocksCleared).IsTrue();
        }

        [global::TUnit.Core.Test]
        public void CloseSymbolsSubsetThrowsWhenMetamethodMissing()
        {
            Script script = new();
            Processor processor = script.GetMainProcessorForTests();
            processor.ClearCallStackForTests();

            SymbolRef symbol = SymbolRef.Local("resource", 0, SymbolRefAttributes.ToBeClosed);
            CallStackItem frame = new()
            {
                LocalScope = new[] { DynValue.NewTable(new Table(script)) },
                BlocksToClose = new List<List<SymbolRef>> { new List<SymbolRef> { symbol } },
                ToBeClosedIndices = new HashSet<int> { 0 },
            };
            processor.PushCallStackFrameForTests(frame);

            ExpectException<ScriptRuntimeException>(() =>
                processor.CloseSymbolsSubsetForTests(frame, new[] { symbol }, DynValue.Nil)
            );
        }

        [global::TUnit.Core.Test]
        public async Task ClearBlockDataClosesSymbolsAndClearsRange()
        {
            Script script = new();
            Processor processor = script.GetMainProcessorForTests();
            processor.ClearCallStackForTests();

            bool closed = false;
            DynValue closable = CreateClosableValue(script, _ => closed = true);

            CallStackItem frame = new()
            {
                LocalScope = new[] { closable, DynValue.NewNumber(7) },
                BlocksToClose = new List<List<SymbolRef>>
                {
                    new List<SymbolRef>
                    {
                        SymbolRef.Local("resource", 0, SymbolRefAttributes.ToBeClosed),
                    },
                },
                ToBeClosedIndices = new HashSet<int> { 0 },
            };
            processor.PushCallStackFrameForTests(frame);

            Instruction instruction = new Instruction(SourceRef.GetClrLocation())
            {
                OpCode = OpCode.Clean,
                NumVal = 0,
                NumVal2 = 1,
                SymbolList = new[]
                {
                    SymbolRef.Local("resource", 0, SymbolRefAttributes.ToBeClosed),
                },
            };

            processor.ClearBlockDataForTests(instruction);

            await Assert.That(closed).IsTrue();
            await Assert.That(frame.LocalScope[0]).IsNull();
            await Assert.That(frame.LocalScope[1]).IsNull();
            bool hasIndices = frame.ToBeClosedIndices != null && frame.ToBeClosedIndices.Count > 0;
            await Assert.That(hasIndices).IsFalse();
            bool blocksCleared = frame.BlocksToClose.All(list => list == null || list.Count == 0);
            await Assert.That(blocksCleared).IsTrue();
        }

        [global::TUnit.Core.Test]
        public async Task ClearBlockDataSkipsWhenRangeInvalid()
        {
            Script script = new();
            Processor processor = script.GetMainProcessorForTests();
            processor.ClearCallStackForTests();

            CallStackItem frame = new()
            {
                LocalScope = new[] { DynValue.NewNumber(1), DynValue.NewNumber(2) },
            };
            processor.PushCallStackFrameForTests(frame);

            Instruction instruction = new Instruction(SourceRef.GetClrLocation())
            {
                OpCode = OpCode.Clean,
                NumVal = 2,
                NumVal2 = 0,
            };

            processor.ClearBlockDataForTests(instruction);

            double[] remaining = frame
                .LocalScope.Where(value => value != null)
                .Select(value => value.Number)
                .ToArray();

            await Assert.That(remaining.Length).IsEqualTo(2);
            await Assert.That(remaining[0]).IsEqualTo(1d);
            await Assert.That(remaining[1]).IsEqualTo(2d);
        }

        private static DynValue CreateClosableValue(Script script, Action<DynValue> onClose = null)
        {
            Table token = new(script);
            Table metatable = new(script);
            metatable.Set(
                "__close",
                DynValue.NewCallback(
                    (ctx, args) =>
                    {
                        if (onClose != null)
                        {
                            DynValue payload = args.Count > 1 ? args[1] : DynValue.Nil;
                            onClose(payload);
                        }

                        return DynValue.Nil;
                    }
                )
            );
            token.MetaTable = metatable;
            return DynValue.NewTable(token);
        }

        private static TException ExpectException<TException>(Action action)
            where TException : Exception
        {
            try
            {
                action();
            }
            catch (TException ex)
            {
                return ex;
            }

            throw new InvalidOperationException(
                $"Expected exception of type {typeof(TException).Name}."
            );
        }
    }
}
