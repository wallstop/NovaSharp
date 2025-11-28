namespace NovaSharp.Interpreter.Tests.TUnit.VM
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Threading.Tasks;
    using global::TUnit.Assertions;
    using NovaSharp.Interpreter;
    using NovaSharp.Interpreter.DataStructs;
    using NovaSharp.Interpreter.DataTypes;
    using NovaSharp.Interpreter.Debugging;
    using NovaSharp.Interpreter.Errors;
    using NovaSharp.Interpreter.Execution.Scopes;
    using NovaSharp.Interpreter.Execution.VM;

    public sealed class ProcessorStackOperationsTUnitTests
    {
        private static readonly double[] DescendingPair = { 3d, 2d };
        private static readonly double[] AscendingTriple = { 2d, 3d, 4d };

        [global::TUnit.Core.Test]
        public async Task ExecIncrClonesReadOnlyValueBeforeIncrement()
        {
            Script script = new();
            Processor processor = script.GetMainProcessorForTests();
            FastStack<DynValue> valueStack = processor.GetValueStackForTests();
            valueStack.Clear();
            valueStack.Push(DynValue.NewNumber(1));
            DynValue readOnlyValue = DynValue.NewNumber(2).AsReadOnly();
            valueStack.Push(readOnlyValue);

            Instruction instruction = new(SourceRef.GetClrLocation()) { NumVal = 1 };
            processor.ExecIncrForTests(instruction);

            DynValue result = valueStack.Peek();

            await Assert.That(result.Number).IsEqualTo(3d);
            await Assert.That(result.ReadOnly).IsFalse();
        }

        [global::TUnit.Core.Test]
        public async Task ExecCNotThrowsWhenConditionIsNotBoolean()
        {
            Script script = new();
            Processor processor = script.GetMainProcessorForTests();
            FastStack<DynValue> valueStack = processor.GetValueStackForTests();
            valueStack.Clear();
            valueStack.Push(DynValue.NewString("not-bool"));
            valueStack.Push(DynValue.NewNumber(1));

            InternalErrorException exception = ExpectException<InternalErrorException>(() =>
                processor.ExecCNotForTests(new Instruction(SourceRef.GetClrLocation()))
            );

            await Assert.That(exception.Message).Contains("CNOT had non-bool arg");
        }

        [global::TUnit.Core.Test]
        public async Task ExecBeginFnClearsExistingScopeTracking()
        {
            Script script = new();
            Processor processor = script.GetMainProcessorForTests();
            processor.ClearCallStackForTests();

            CallStackItem frame = new()
            {
                BlocksToClose = new List<List<SymbolRef>> { new List<SymbolRef>() },
                ToBeClosedIndices = new HashSet<int> { 0 },
                LocalScope = Array.Empty<DynValue>(),
                ClosureScope = new ClosureContext(),
            };
            processor.PushCallStackFrameForTests(frame);

            Instruction instruction = new Instruction(SourceRef.GetClrLocation())
            {
                NumVal = 1,
                NumVal2 = 0,
                SymbolList = new[] { SymbolRef.Local("tmp", 0) },
            };

            processor.ExecBeginFnForTests(instruction);

            await Assert.That(frame.BlocksToClose).IsNull();
            await Assert.That(frame.ToBeClosedIndices).IsNull();
            await Assert.That(frame.LocalScope).IsNotNull();
        }

        [global::TUnit.Core.Test]
        public async Task PopExecStackAndCheckVStackReturnsReturnAddressWhenGuardMatches()
        {
            Script script = new();
            Processor processor = script.GetMainProcessorForTests();
            processor.ClearCallStackForTests();
            processor.PushCallStackFrameForTests(
                new CallStackItem() { BasePointer = 3, ReturnAddress = 42 }
            );

            int returnAddress = processor.PopExecStackAndCheckVStackForTests(3);
            await Assert.That(returnAddress).IsEqualTo(42);
        }

        [global::TUnit.Core.Test]
        public void PopExecStackAndCheckVStackThrowsWhenGuardDiffers()
        {
            Script script = new();
            Processor processor = script.GetMainProcessorForTests();
            processor.ClearCallStackForTests();
            processor.PushCallStackFrameForTests(
                new CallStackItem() { BasePointer = 2, ReturnAddress = 7 }
            );

            ExpectException<InternalErrorException>(() =>
                processor.PopExecStackAndCheckVStackForTests(0)
            );
        }

        [global::TUnit.Core.Test]
        public async Task SetGlobalSymbolThrowsWhenEnvIsNotTable()
        {
            InvalidOperationException exception = ExpectException<InvalidOperationException>(() =>
                Processor.SetGlobalSymbolForTests(
                    DynValue.NewString("not-table"),
                    "value",
                    DynValue.NewNumber(1)
                )
            );

            await Assert.That(exception.Message).Contains("_ENV is not a table");
        }

        [global::TUnit.Core.Test]
        public async Task SetGlobalSymbolStoresValueAndTreatsNullAsNil()
        {
            Script script = new();
            DynValue env = DynValue.NewTable(script.Globals);

            Processor.SetGlobalSymbolForTests(env, "answer", DynValue.NewNumber(42));
            await Assert.That(env.Table.Get("answer").Number).IsEqualTo(42d);

            Processor.SetGlobalSymbolForTests(env, "cleared", null);
            await Assert.That(env.Table.Get("cleared").IsNil()).IsTrue();
        }

        [global::TUnit.Core.Test]
        public void AssignGenericSymbolRejectsDefaultEnvSymbol()
        {
            Script script = new();
            Processor processor = script.GetMainProcessorForTests();

            ExpectException<ArgumentException>(() =>
                processor.AssignGenericSymbol(SymbolRef.DefaultEnv, DynValue.NewNumber(1))
            );
        }

        [global::TUnit.Core.Test]
        public async Task AssignGenericSymbolUpdatesGlobalScope()
        {
            Script script = new();
            Processor processor = script.GetMainProcessorForTests();

            SymbolRef global = SymbolRef.Global("greeting", SymbolRef.DefaultEnv);
            processor.AssignGenericSymbol(global, DynValue.NewString("hello"));

            await Assert.That(script.Globals.Get("greeting").String).IsEqualTo("hello");
        }

        [global::TUnit.Core.Test]
        public async Task AssignGenericSymbolUpdatesLocalScope()
        {
            Script script = new();
            Processor processor = script.GetMainProcessorForTests();
            processor.ClearCallStackForTests();

            CallStackItem frame = new()
            {
                LocalScope = new[] { DynValue.NewNil() },
                ClosureScope = new ClosureContext(),
            };
            processor.PushCallStackFrameForTests(frame);

            processor.AssignGenericSymbol(SymbolRef.Local("value", 0), DynValue.NewNumber(7));
            await Assert.That(frame.LocalScope[0].Number).IsEqualTo(7d);
        }

        [global::TUnit.Core.Test]
        public async Task AssignGenericSymbolUpdatesUpValueScope()
        {
            Script script = new();
            Processor processor = script.GetMainProcessorForTests();
            processor.ClearCallStackForTests();

            ClosureContext closure = new();
            closure.Add(DynValue.NewNil());
            CallStackItem frame = new()
            {
                LocalScope = Array.Empty<DynValue>(),
                ClosureScope = closure,
            };
            processor.PushCallStackFrameForTests(frame);

            processor.AssignGenericSymbol(SymbolRef.UpValue("uv", 0), DynValue.NewNumber(11));
            await Assert.That(frame.ClosureScope[0].Number).IsEqualTo(11d);
        }

        [global::TUnit.Core.Test]
        public void AssignGenericSymbolThrowsForUnexpectedSymbolType()
        {
            Script script = new();
            Processor processor = script.GetMainProcessorForTests();

            SymbolRef invalid = SymbolRef.Local("value", 0);
            invalid.SymbolType = (SymbolRefType)999;

            ExpectException<InternalErrorException>(() =>
                processor.AssignGenericSymbol(invalid, DynValue.NewNumber(1))
            );
        }

        [global::TUnit.Core.Test]
        public void GetGenericSymbolThrowsForUnexpectedSymbolType()
        {
            Script script = new();
            Processor processor = script.GetMainProcessorForTests();

            SymbolRef invalid = SymbolRef.Local("value", 0);
            invalid.SymbolType = (SymbolRefType)999;

            ExpectException<InternalErrorException>(() => processor.GetGenericSymbol(invalid));
        }

        [global::TUnit.Core.Test]
        public async Task StackTopToArrayReturnsValuesWithoutAlteringStackWhenNotPopping()
        {
            Script script = new();
            Processor processor = script.GetMainProcessorForTests();
            FastStack<DynValue> stack = processor.GetValueStackForTests();
            stack.Clear();
            stack.Push(DynValue.NewNumber(1));
            stack.Push(DynValue.NewNumber(2));
            stack.Push(DynValue.NewNumber(3));

            DynValue[] peeked = processor.StackTopToArrayForTests(2, pop: false);
            double[] peekedNumbers = peeked.Select(v => v.Number).ToArray();
            await Assert.That(peekedNumbers.Length).IsEqualTo(DescendingPair.Length);
            await Assert.That(peekedNumbers[0]).IsEqualTo(DescendingPair[0]);
            await Assert.That(peekedNumbers[1]).IsEqualTo(DescendingPair[1]);
            await Assert.That(stack.Count).IsEqualTo(3);

            DynValue[] popped = processor.StackTopToArrayForTests(2, pop: true);
            double[] poppedNumbers = popped.Select(v => v.Number).ToArray();
            await Assert.That(poppedNumbers.Length).IsEqualTo(DescendingPair.Length);
            await Assert.That(poppedNumbers[0]).IsEqualTo(DescendingPair[0]);
            await Assert.That(poppedNumbers[1]).IsEqualTo(DescendingPair[1]);
            await Assert.That(stack.Count).IsEqualTo(1);
        }

        [global::TUnit.Core.Test]
        public async Task StackTopToArrayReverseReturnsValuesInAscendingOrder()
        {
            Script script = new();
            Processor processor = script.GetMainProcessorForTests();
            FastStack<DynValue> stack = processor.GetValueStackForTests();
            stack.Clear();
            stack.Push(DynValue.NewNumber(1));
            stack.Push(DynValue.NewNumber(2));
            stack.Push(DynValue.NewNumber(3));
            stack.Push(DynValue.NewNumber(4));

            DynValue[] peeked = processor.StackTopToArrayReverseForTests(3, pop: false);
            double[] peekedNumbers = peeked.Select(v => v.Number).ToArray();
            await Assert.That(peekedNumbers.Length).IsEqualTo(AscendingTriple.Length);
            await Assert.That(peekedNumbers[0]).IsEqualTo(AscendingTriple[0]);
            await Assert.That(peekedNumbers[1]).IsEqualTo(AscendingTriple[1]);
            await Assert.That(peekedNumbers[2]).IsEqualTo(AscendingTriple[2]);
            await Assert.That(stack.Count).IsEqualTo(4);

            DynValue[] popped = processor.StackTopToArrayReverseForTests(3, pop: true);
            double[] poppedNumbers = popped.Select(v => v.Number).ToArray();
            await Assert.That(poppedNumbers.Length).IsEqualTo(AscendingTriple.Length);
            await Assert.That(poppedNumbers[0]).IsEqualTo(AscendingTriple[0]);
            await Assert.That(poppedNumbers[1]).IsEqualTo(AscendingTriple[1]);
            await Assert.That(poppedNumbers[2]).IsEqualTo(AscendingTriple[2]);
            await Assert.That(stack.Count).IsEqualTo(1);
        }

        [global::TUnit.Core.Test]
        public void PushCallStackFrameForTestsRejectsNullFrames()
        {
            Script script = new();
            Processor processor = script.GetMainProcessorForTests();

            ExpectException<ArgumentNullException>(() =>
                processor.PushCallStackFrameForTests(null)
            );
        }

        [global::TUnit.Core.Test]
        public async Task ClearCallStackForTestsEmptiesExistingFrames()
        {
            Script script = new();
            Processor processor = script.GetMainProcessorForTests();

            processor.PushCallStackFrameForTests(new CallStackItem());
            processor.PushCallStackFrameForTests(new CallStackItem());

            processor.ClearCallStackForTests();

            FastStack<CallStackItem> executionStack = processor.GetExecutionStackForTests();
            await Assert.That(executionStack.Count).IsEqualTo(0);
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
