namespace NovaSharp.Interpreter.Tests.TUnit.VM
{
    using System;
    using System.Collections.Generic;
    using System.Threading;
    using System.Threading.Tasks;
    using global::TUnit.Assertions;
    using NovaSharp;
    using NovaSharp.Interpreter;
    using NovaSharp.Interpreter.DataTypes;
    using NovaSharp.Interpreter.Debugging;
    using NovaSharp.Interpreter.Errors;
    using NovaSharp.Interpreter.Execution.VM;
    using NovaSharp.Interpreter.Interop;
    using NovaSharp.Interpreter.Modules;

    public sealed class ProcessorCoreLifecycleTUnitTests
    {
        [global::TUnit.Core.Test]
        public async Task CallThrowsWhenEnteredFromDifferentThread()
        {
            Script script = new();
            script.Options.CheckThreadAccess = true;
            DynValue chunk = script.LoadString("return 42");

            Processor processor = script.GetMainProcessorForTests();
            processor.SetThreadOwnershipStateForTests(
                Environment.CurrentManagedThreadId,
                executionNesting: 1
            );

            InvalidOperationException observed = null;
            Thread worker = new(() =>
            {
                try
                {
                    script.Call(chunk);
                }
                catch (InvalidOperationException ex)
                {
                    observed = ex;
                }
            })
            {
                IsBackground = true,
            };
            worker.Start();
            worker.Join();

            await Assert.That(observed).IsNotNull();
            await Assert
                .That(observed.Message)
                .Contains("Cannot enter the same NovaSharp processor");

            processor.SetThreadOwnershipStateForTests(-1, executionNesting: 0);
            script.Options.CheckThreadAccess = false;
        }

        [global::TUnit.Core.Test]
        public async Task EnterAndLeaveProcessorUpdateParentCoroutineStack()
        {
            Script script = new();
            Processor parent = script.GetMainProcessorForTests();
            Processor child = Processor.CreateChildProcessorForTests(parent);

            List<Processor> stack = parent.GetCoroutineStackForTests();

            child.EnterProcessorForTests();
            await Assert.That(stack[^1] == child).IsTrue();

            child.LeaveProcessorForTests();
            await Assert.That(stack.Count).IsEqualTo(0);
        }

        [global::TUnit.Core.Test]
        public async Task PerformMessageDecorationBeforeUnwindUsesClrFunction()
        {
            Script script = new();
            Processor processor = script.GetMainProcessorForTests();

            bool invoked = false;
            DynValue handler = DynValue.NewCallback(
                (ctx, args) =>
                {
                    invoked = true;
                    return DynValue.NewString("decorated");
                }
            );

            string result = processor.PerformMessageDecorationBeforeUnwind(
                handler,
                "boom",
                SourceRef.GetClrLocation()
            );

            await Assert.That(invoked).IsTrue();
            await Assert.That(result).IsEqualTo("decorated");
        }

        [global::TUnit.Core.Test]
        public async Task PerformMessageDecorationBeforeUnwindThrowsWhenHandlerIsNotFunction()
        {
            Script script = new();
            Processor processor = script.GetMainProcessorForTests();
            DynValue handler = DynValue.NewNumber(42);

            string result = processor.PerformMessageDecorationBeforeUnwind(
                handler,
                "boom",
                SourceRef.GetClrLocation()
            );

            await Assert.That(result).IsEqualTo("error handler not set to a function.boom");
        }

        [global::TUnit.Core.Test]
        public async Task PerformMessageDecorationBeforeUnwindAppendsInnerExceptionMessage()
        {
            Script script = new();
            Processor processor = script.GetMainProcessorForTests();

            DynValue handler = DynValue.NewCallback(
                (ctx, args) =>
                {
                    throw new ScriptRuntimeException("decorator failure");
                }
            );

            string decorated = processor.PerformMessageDecorationBeforeUnwind(
                handler,
                "boom",
                SourceRef.GetClrLocation()
            );

            await Assert.That(decorated).IsEqualTo("decorator failure.boom");
        }

        [global::TUnit.Core.Test]
        public async Task ParentConstructorInitializesState()
        {
            Script script = new();
            Processor parent = script.GetMainProcessorForTests();
            Processor child = Processor.CreateChildProcessorForTests(parent);

            await Assert.That(child.ParentProcessorForTests == parent).IsTrue();
            await Assert.That(child.State).IsEqualTo(CoroutineState.NotStarted);
        }

        [global::TUnit.Core.Test]
        public async Task RecycleConstructorReusesStacks()
        {
            Script script = new();
            Processor parent = script.GetMainProcessorForTests();
            Processor recycleSource = Processor.CreateChildProcessorForTests(parent);

            Processor recycled = Processor.CreateRecycledProcessorForTests(parent, recycleSource);

            await Assert
                .That(
                    ReferenceEquals(
                        recycled.GetValueStackForTests(),
                        recycleSource.GetValueStackForTests()
                    )
                )
                .IsTrue();
            await Assert
                .That(
                    ReferenceEquals(
                        recycled.GetExecutionStackForTests(),
                        recycleSource.GetExecutionStackForTests()
                    )
                )
                .IsTrue();
        }

        [global::TUnit.Core.Test]
        public async Task CallDelegatesToParentCoroutineStackTop()
        {
            Script script = new();
            DynValue function = script.LoadString("return 321");

            Processor parent = script.GetMainProcessorForTests();
            Processor child = Processor.CreateChildProcessorForTests(parent);
            Processor delegated = Processor.CreateChildProcessorForTests(parent);

            parent.ReplaceCoroutineStackForTests(new List<Processor> { delegated });

            DynValue result = child.Call(function, Array.Empty<DynValue>());

            await Assert.That(result.Number).IsEqualTo(321d);
        }

        [global::TUnit.Core.Test]
        public async Task CoroutineYieldPassesValuesWhenYieldingIsAllowed()
        {
            Script script = new(CoreModules.PresetComplete);
            script.DoString(
                @"
                function worker()
                    coroutine.yield('pause')
                    return 'done'
                end
            "
            );

            DynValue coroutineValue = script.CreateCoroutine(script.Globals.Get("worker"));

            DynValue yielded = coroutineValue.Coroutine.Resume();

            await Assert.That(yielded.Type).IsEqualTo(DataType.String);
            await Assert.That(yielded.String).IsEqualTo("pause");
            await Assert.That(coroutineValue.Coroutine.State).IsEqualTo(CoroutineState.Suspended);
        }

        [global::TUnit.Core.Test]
        public async Task YieldingFromMainChunkThrowsCannotYieldMain()
        {
            Script script = new(CoreModules.PresetComplete);

            ScriptRuntimeException exception = ExpectException<ScriptRuntimeException>(() =>
                script.DoString("coroutine.yield('outside')")
            );

            await Assert
                .That(exception.Message)
                .Contains("attempt to yield from outside a coroutine");
        }

        [global::TUnit.Core.Test]
        public async Task YieldingWithClrBoundaryInsideCoroutineThrowsCannotYield()
        {
            Script script = new(CoreModules.PresetComplete);
            script.DoString(
                @"
                function boundary()
                    coroutine.yield('pause')
                    return 'done'
                end
            "
            );

            DynValue coroutineValue = script.CreateCoroutine(script.Globals.Get("boundary"));
            Processor coroutineProcessor = coroutineValue.Coroutine.GetProcessorForTests();
            bool originalCanYield = coroutineProcessor.SwapCanYieldForTests(false);

            try
            {
                ScriptRuntimeException exception = ExpectException<ScriptRuntimeException>(() =>
                    coroutineValue.Coroutine.Resume()
                );
                await Assert
                    .That(exception.Message)
                    .Contains("attempt to yield across a CLR-call boundary");
            }
            finally
            {
                coroutineProcessor.SwapCanYieldForTests(originalCanYield);
            }
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
