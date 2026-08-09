namespace WallstopStudios.NovaSharp.Interpreter.Tests.TUnit.Units.Execution.ProcessorExecution
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Threading.Tasks;
    using global::NovaSharp;
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

            SymbolRef resource = SymbolRef.Local("resource", 0, SymbolRefAttributes.ToBeClosed);
            SymbolRef nilResource = SymbolRef.Local(
                "nilResource",
                1,
                SymbolRefAttributes.ToBeClosed
            );
            SymbolRef falseResource = SymbolRef.Local(
                "falseResource",
                2,
                SymbolRefAttributes.ToBeClosed
            );
            int closeCount = 0;
            LuaValue closable = CreateClosableValue(script, _ => closeCount++);

            CallStackItem frame = new()
            {
                LocalScope = new[]
                {
                    new ValueSlot(closable),
                    new ValueSlot(LuaValue.Nil),
                    new ValueSlot(LuaValue.False),
                },
                BlocksToClose = new List<List<SymbolRef>>
                {
                    new List<SymbolRef> { resource, nilResource, falseResource },
                },
                ToBeClosedIndices = new HashSet<int> { 0, 1, 2 },
            };
            UpvalueCell captured = frame.LocalScope[0].Capture();
            processor.PushCallStackFrameForTests(frame);

            processor.CloseSymbolsSubsetForTests(
                frame,
                new[] { resource, nilResource, falseResource },
                LuaValue.NewString("err")
            );

            await Assert.That(closeCount).IsEqualTo(1);
            await Assert.That(frame.LocalScope[0].IsActive).IsFalse();
            await Assert.That(frame.LocalScope[1].IsActive).IsFalse();
            await Assert.That(frame.LocalScope[2].IsActive).IsFalse();
            await Assert.That(captured.Value.Table).IsSameReferenceAs(closable.Table);
            bool containsIndex =
                frame.ToBeClosedIndices != null && frame.ToBeClosedIndices.Contains(0);
            await Assert.That(containsIndex).IsFalse();
            bool blocksCleared =
                frame.BlocksToClose == null
                || frame.BlocksToClose.All(list => list == null || list.Count == 0);
            await Assert.That(blocksCleared).IsTrue();

            processor.CloseSymbolsSubsetForTests(
                frame,
                new[] { resource, nilResource, falseResource },
                LuaValue.NewString("err")
            );
            await Assert.That(closeCount).IsEqualTo(1);
        }

        [global::TUnit.Core.Test]
        public async Task CloseSymbolsSubsetThrowsWhenMetamethodMissing()
        {
            Script script = new();
            Processor processor = script.GetMainProcessorForTests();
            processor.ClearCallStackForTests();

            SymbolRef symbol = SymbolRef.Local("resource", 0, SymbolRefAttributes.ToBeClosed);
            LuaValue valueWithoutCloseMetamethod = LuaValue.NewTable(new Table(script));
            CallStackItem frame = new()
            {
                LocalScope = new[] { new ValueSlot(valueWithoutCloseMetamethod) },
                BlocksToClose = new List<List<SymbolRef>> { new List<SymbolRef> { symbol } },
                ToBeClosedIndices = new HashSet<int> { 0 },
            };
            UpvalueCell captured = frame.LocalScope[0].Capture();
            processor.PushCallStackFrameForTests(frame);

            ExpectException<ScriptRuntimeException>(() =>
                processor.CloseSymbolsSubsetForTests(frame, new[] { symbol }, LuaValue.Nil)
            );
            await Assert.That(frame.LocalScope[0].IsActive).IsFalse();
            await Assert
                .That(captured.Value.Table)
                .IsSameReferenceAs(valueWithoutCloseMetamethod.Table);

            processor.CloseSymbolsSubsetForTests(frame, new[] { symbol }, LuaValue.Nil);
        }

        [global::TUnit.Core.Test]
        public async Task ClearBlockDataClosesSymbolsAndClearsRange()
        {
            Script script = new();
            Processor processor = script.GetMainProcessorForTests();
            processor.ClearCallStackForTests();

            int closeCount = 0;
            LuaValue closable = CreateClosableValue(script, _ => closeCount++);

            CallStackItem frame = new()
            {
                LocalScope = new[]
                {
                    new ValueSlot(closable),
                    new ValueSlot(LuaValue.NewNumber(7)),
                },
                BlocksToClose = new List<List<SymbolRef>>
                {
                    new List<SymbolRef>
                    {
                        SymbolRef.Local("resource", 0, SymbolRefAttributes.ToBeClosed),
                    },
                },
                ToBeClosedIndices = new HashSet<int> { 0 },
            };
            UpvalueCell captured = frame.LocalScope[0].Capture();
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

            await Assert.That(closeCount).IsEqualTo(1);
            await Assert.That(frame.LocalScope[0].IsActive).IsFalse();
            await Assert.That(frame.LocalScope[1].IsActive).IsFalse();
            await Assert.That(captured.Value.Table).IsSameReferenceAs(closable.Table);
            bool hasIndices = frame.ToBeClosedIndices != null && frame.ToBeClosedIndices.Count > 0;
            await Assert.That(hasIndices).IsFalse();
            bool blocksCleared = frame.BlocksToClose.All(list => list == null || list.Count == 0);
            await Assert.That(blocksCleared).IsTrue();

            processor.ClearBlockDataForTests(instruction);
            await Assert.That(closeCount).IsEqualTo(1);
        }

        [global::TUnit.Core.Test]
        public async Task ClearBlockDataSkipsWhenRangeInvalid()
        {
            Script script = new();
            Processor processor = script.GetMainProcessorForTests();
            processor.ClearCallStackForTests();

            CallStackItem frame = new()
            {
                LocalScope = new[]
                {
                    new ValueSlot(LuaValue.NewNumber(1)),
                    new ValueSlot(LuaValue.NewNumber(2)),
                },
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
                .LocalScope.Where(slot => slot.IsActive)
                .Select(slot => slot.Value.Number)
                .ToArray();

            await Assert.That(remaining.Length).IsEqualTo(2);
            await Assert.That(remaining[0]).IsEqualTo(1d);
            await Assert.That(remaining[1]).IsEqualTo(2d);
        }

        private static LuaValue CreateClosableValue(Script script, Action<LuaValue> onClose = null)
        {
            Table token = new(script);
            Table metatable = new(script);
            metatable.Set(
                "__close",
                LuaValue.NewCallback(
                    (ctx, args) =>
                    {
                        if (onClose != null)
                        {
                            LuaValue payload = args.Count > 1 ? args[1] : LuaValue.Nil;
                            onClose(payload);
                        }

                        return LuaValue.Nil;
                    }
                )
            );
            token.MetaTable = metatable;
            return LuaValue.NewTable(token);
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
