namespace NovaSharp.Interpreter.Tests.TUnit.VM
{
    using System;
    using System.Threading.Tasks;
    using global::TUnit.Assertions;
    using NovaSharp.Interpreter;
    using NovaSharp.Interpreter.DataStructs;
    using NovaSharp.Interpreter.DataTypes;
    using NovaSharp.Interpreter.Debugging;
    using NovaSharp.Interpreter.Errors;
    using NovaSharp.Interpreter.Execution.Scopes;
    using NovaSharp.Interpreter.Execution.VM;

    public sealed class ProcessorBitwiseAndReturnTUnitTests
    {
        [global::TUnit.Core.Test]
        public void ExecRetThrowsWhenReturningMoreThanOneValue()
        {
            Script script = new();
            Processor processor = script.GetMainProcessorForTests();
            processor.ClearCallStackForTests();

            CallStackItem frame = new()
            {
                ReturnAddress = 0,
                LocalScope = Array.Empty<DynValue>(),
                ClosureScope = new ClosureContext(),
            };
            processor.PushCallStackFrameForTests(frame);

            Instruction instruction = new Instruction(SourceRef.GetClrLocation()) { NumVal = 2 };

            ExpectException<InternalErrorException>(() => processor.ExecRetForTests(instruction));
        }

        [global::TUnit.Core.Test]
        public async Task ExecBitNotThrowsWhenOperandIsNotInteger()
        {
            Script script = new();
            Processor processor = script.GetMainProcessorForTests();
            FastStack<DynValue> stack = processor.GetValueStackForTests();
            stack.Clear();
            stack.Push(DynValue.NewString("not-integer"));

            ScriptRuntimeException exception = ExpectException<ScriptRuntimeException>(() =>
                processor.ExecBitNotForTests(new Instruction(SourceRef.GetClrLocation()), 0)
            );

            await Assert.That(exception.Message).Contains("bitwise");
        }

        [global::TUnit.Core.Test]
        public async Task ExecBitAndThrowsWhenOperandsAreNotIntegers()
        {
            Script script = new();
            Processor processor = script.GetMainProcessorForTests();
            FastStack<DynValue> stack = processor.GetValueStackForTests();
            stack.Clear();
            stack.Push(DynValue.NewNumber(1));
            stack.Push(DynValue.NewString("bad"));

            ScriptRuntimeException exception = ExpectException<ScriptRuntimeException>(() =>
                processor.ExecBitAndForTests(new Instruction(SourceRef.GetClrLocation()), 0)
            );

            await Assert.That(exception.Message).Contains("bitwise");
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
