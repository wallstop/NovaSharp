namespace WallstopStudios.NovaSharp.Interpreter.Tests.TUnit.Units.Execution.ProcessorExecution
{
    using System;
    using System.Collections.Generic;
    using System.Threading.Tasks;
    using global::TUnit.Assertions;
    using WallstopStudios.NovaSharp.Interpreter;
    using WallstopStudios.NovaSharp.Interpreter.DataTypes;
    using WallstopStudios.NovaSharp.Interpreter.Debugging;
    using WallstopStudios.NovaSharp.Interpreter.Errors;
    using WallstopStudios.NovaSharp.Interpreter.Execution.VM;
    using WallstopStudios.NovaSharp.Tests.TestInfrastructure.Scopes;
    using static ProcessorDebuggerTestHelpers;

    public sealed class ProcessorDebuggerRuntimeTUnitTests
    {
        [global::TUnit.Core.Test]
        public async Task RefreshDebuggerThreadsUsesParentCoroutineStack()
        {
            Script script = new();
            script.DoString("function idle() return 5 end");

            DynValue coroutineValue = script.CreateCoroutine(script.Globals.Get("idle"));
            Coroutine coroutine = coroutineValue.Coroutine;
            Processor coroutineProcessor = coroutine.GetProcessorForTests();

            Processor parentProcessor = script.GetMainProcessorForTests();
            List<Processor> parentStack = parentProcessor.GetCoroutineStackForTests();
            using ProcessorCoroutineStackScope stackScope = ProcessorCoroutineStackScope.Capture(
                parentProcessor
            );

            parentStack.Clear();
            parentStack.Add(coroutineProcessor);

            List<WatchItem> threads = coroutineProcessor.RefreshDebuggerThreadsForTests();

            await Assert.That(threads.Count).IsEqualTo(1);
            await Assert.That(threads[0].Address).IsEqualTo(coroutine.ReferenceId);
            await Assert.That(threads[0].Name).IsEqualTo($"coroutine #{coroutine.ReferenceId}");
        }

        [global::TUnit.Core.Test]
        public async Task RuntimeExceptionRefreshesDebuggerWhenSignalRequestsPause()
        {
            Script script = new();
            Processor processor = script.GetMainProcessorForTests();
            PrepareCallStack(processor);

            StubDebugger debugger = new() { SignalRuntimeExceptionResult = true };
            debugger.EnqueueAction(DebuggerAction.ActionType.Run);
            processor.AttachDebuggerForTests(debugger, lineBasedBreakpoints: false);
            processor.DebuggerEnabled = true;

            const string chunk =
                @"
                local function explode()
                    error('boom')
                end
                explode()
            ";

            ScriptRuntimeException exception = ExpectException<ScriptRuntimeException>(() =>
                script.DoString(chunk)
            );
            await Assert.That(exception).IsNotNull();
            await Assert.That(debugger.SignalRuntimeExceptionCallCount).IsEqualTo(1);
            await Assert.That(debugger.UpdateCallCount > 0).IsTrue();
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
