namespace WallstopStudios.NovaSharp.Interpreter.Tests.TUnit.Units.Execution.ProcessorExecution
{
    using System;
    using System.Collections.Generic;
    using System.Threading;
    using System.Threading.Tasks;
    using global::TUnit.Assertions;
    using NovaSharp;
    using WallstopStudios.NovaSharp.Interpreter;
    using WallstopStudios.NovaSharp.Interpreter.Compatibility;
    using WallstopStudios.NovaSharp.Interpreter.DataStructs;
    using WallstopStudios.NovaSharp.Interpreter.DataTypes;
    using WallstopStudios.NovaSharp.Interpreter.Debugging;
    using WallstopStudios.NovaSharp.Interpreter.Errors;
    using WallstopStudios.NovaSharp.Interpreter.Execution.VM;
    using WallstopStudios.NovaSharp.Interpreter.Interop;
    using WallstopStudios.NovaSharp.Interpreter.Modules;
    using WallstopStudios.NovaSharp.Tests.TestInfrastructure.Scopes;
    using WallstopStudios.NovaSharp.Tests.TestInfrastructure.TUnit;

    public sealed class ProcessorCoreLifecycleTUnitTests
    {
        private static readonly TimeSpan WorkerJoinTimeout = TimeSpan.FromSeconds(10);
        private const int DeepNonTailCallFixtureDepth = 80;
        private const int LargeVarargFixtureCount = 528;

        [global::TUnit.Core.Test]
        [AllLuaVersions]
        public async Task CallThrowsWhenEnteredFromDifferentThread(LuaCompatibilityVersion version)
        {
            Script script = new(version);
            script.Options.CheckThreadAccess = true;
            DynValue chunk = script.LoadString("return 42");

            Processor processor = script.GetMainProcessorForTests();
            processor.SetThreadOwnershipStateForTests(
                Environment.CurrentManagedThreadId,
                executionNesting: 1
            );
            using DeferredActionScope cleanup = DeferredActionScope.Run(() =>
            {
                processor.SetThreadOwnershipStateForTests(-1, executionNesting: 0);
                script.Options.CheckThreadAccess = false;
            });

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
            JoinWorker(worker);

            await Assert.That(observed).IsNotNull();
            await Assert
                .That(observed.Message)
                .Contains("Cannot enter the same NovaSharp processor");
        }

        [global::TUnit.Core.Test]
        [AllLuaVersions]
        public async Task NestedEntryKeepsThreadOwnershipUntilOutermostLeave(
            LuaCompatibilityVersion version
        )
        {
            Script script = new(version);
            script.Options.CheckThreadAccess = true;
            DynValue chunk = script.LoadString("return 42");
            Processor processor = script.GetMainProcessorForTests();

            processor.EnterProcessorForTests();
            processor.EnterProcessorForTests();
            processor.LeaveProcessorForTests();
            using DeferredActionScope cleanup = DeferredActionScope.Run(() =>
            {
                processor.LeaveProcessorForTests();
                script.Options.CheckThreadAccess = false;
            });

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
            JoinWorker(worker);

            await Assert.That(observed).IsNotNull().ConfigureAwait(false);
            await Assert
                .That(observed.Message)
                .Contains("Cannot enter the same NovaSharp processor")
                .ConfigureAwait(false);
        }

        private static void JoinWorker(Thread worker)
        {
            if (!worker.Join(WorkerJoinTimeout))
            {
                throw new TimeoutException("Worker thread did not finish.");
            }
        }

        private static int GetDeepNonTailCallDepth() =>
            VmStackDefaults.ExecutionStackInitialCapacity + 16;

        private static int GetLargeVarargCount() => VmStackDefaults.ValueStackInitialCapacity + 16;

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
            // Per Lua spec: when error handler fails (including when not callable),
            // the result is "error in error handling"
            Script script = new();
            Processor processor = script.GetMainProcessorForTests();
            DynValue handler = DynValue.NewNumber(42);

            string result = processor.PerformMessageDecorationBeforeUnwind(
                handler,
                "boom",
                SourceRef.GetClrLocation()
            );

            await Assert.That(result).IsEqualTo("error in error handling").ConfigureAwait(false);
        }

        [global::TUnit.Core.Test]
        public async Task PerformMessageDecorationBeforeUnwindAppendsInnerExceptionMessage()
        {
            // Per Lua spec: when error handler throws an error, the result is "error in error handling"
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

            await Assert.That(decorated).IsEqualTo("error in error handling").ConfigureAwait(false);
        }

        [global::TUnit.Core.Test]
        public async Task ParentConstructorInitializesState()
        {
            Script script = new();
            Processor parent = script.GetMainProcessorForTests();
            Processor child = Processor.CreateChildProcessorForTests(parent);

            await Assert.That(child.ParentProcessorForTests == parent).IsTrue();
            await Assert.That(child.State).IsEqualTo(CoroutineState.NotStarted);
            await Assert
                .That(child.GetValueStackForTests().Capacity)
                .IsEqualTo(VmStackDefaults.ValueStackInitialCapacity);
            await Assert
                .That(child.GetExecutionStackForTests().Capacity)
                .IsEqualTo(VmStackDefaults.ExecutionStackInitialCapacity);
        }

        [global::TUnit.Core.Test]
        public async Task MainProcessorUsesSmallGrowableStacks()
        {
            Script script = new();
            Processor processor = script.GetMainProcessorForTests();
            FastStack<DynValue> valueStack = processor.GetValueStackForTests();
            FastStack<CallStackItem> executionStack = processor.GetExecutionStackForTests();

            await Assert
                .That(valueStack.Capacity)
                .IsEqualTo(VmStackDefaults.ValueStackInitialCapacity);
            await Assert
                .That(executionStack.Capacity)
                .IsEqualTo(VmStackDefaults.ExecutionStackInitialCapacity);

            for (int i = 0; i <= VmStackDefaults.ValueStackInitialCapacity; i++)
            {
                valueStack.Push(DynValue.NewNumber(i));
            }

            for (int i = 0; i <= VmStackDefaults.ExecutionStackInitialCapacity; i++)
            {
                executionStack.Push(new CallStackItem());
            }

            await Assert
                .That(valueStack.Count)
                .IsEqualTo(VmStackDefaults.ValueStackInitialCapacity + 1);
            await Assert
                .That(valueStack.Capacity)
                .IsGreaterThan(VmStackDefaults.ValueStackInitialCapacity);
            await Assert
                .That(executionStack.Count)
                .IsEqualTo(VmStackDefaults.ExecutionStackInitialCapacity + 1);
            await Assert
                .That(executionStack.Capacity)
                .IsGreaterThan(VmStackDefaults.ExecutionStackInitialCapacity);
        }

        [global::TUnit.Core.Test]
        public async Task CreatedCoroutineUsesSmallGrowableStacks()
        {
            Script script = new();
            DynValue function = script.LoadString("return 1");

            DynValue coroutineValue = script.CreateCoroutine(function);
            Processor coroutineProcessor = coroutineValue.Coroutine.GetProcessorForTests();

            await Assert
                .That(
                    coroutineProcessor.ParentProcessorForTests == script.GetMainProcessorForTests()
                )
                .IsTrue();
            await Assert.That(coroutineProcessor.State).IsEqualTo(CoroutineState.NotStarted);
            await Assert
                .That(coroutineProcessor.GetValueStackForTests().Capacity)
                .IsEqualTo(VmStackDefaults.ValueStackInitialCapacity);
            await Assert
                .That(coroutineProcessor.GetExecutionStackForTests().Capacity)
                .IsEqualTo(VmStackDefaults.ExecutionStackInitialCapacity);
        }

        [global::TUnit.Core.Test]
        [AllLuaVersions]
        public async Task NonTailRecursionGrowsExecutionStackPastInitialCapacity(
            LuaCompatibilityVersion version
        )
        {
            Script script = new(version);
            Processor processor = script.GetMainProcessorForTests();

            // Keep the fixture-extracted Lua literal-only; these asserts catch capacity drift.
            int configuredDepth = GetDeepNonTailCallDepth();
            await Assert.That(configuredDepth).IsEqualTo(DeepNonTailCallFixtureDepth);

            DynValue result = script.DoString(
                @"
                local function recurse(n)
                    if n == 0 then
                        return 1
                    end

                    return 1 + recurse(n - 1)
                end

                return recurse(80)
            "
            );

            await Assert.That(result.Number).IsEqualTo(configuredDepth + 1d);
            await Assert
                .That(processor.GetExecutionStackForTests().Capacity)
                .IsGreaterThan(VmStackDefaults.ExecutionStackInitialCapacity);
        }

        [global::TUnit.Core.Test]
        [AllLuaVersions]
        public async Task LargeVarargCallGrowsValueStackPastInitialCapacity(
            LuaCompatibilityVersion version
        )
        {
            Script script = new(version);
            Processor processor = script.GetMainProcessorForTests();

            // Keep the fixture-extracted Lua literal-only; these asserts catch capacity drift.
            int configuredCount = GetLargeVarargCount();
            await Assert.That(configuredCount).IsEqualTo(LargeVarargFixtureCount);

            DynValue result = script.DoString(
                @"
                local function count(...)
                    return select('#', ...)
                end

                local function values(n)
                    if n == 0 then
                        return
                    end

                    return n, values(n - 1)
                end

                return count(values(528))
            "
            );

            await Assert.That(result.Number).IsEqualTo((double)configuredCount);
            await Assert
                .That(processor.GetValueStackForTests().Capacity)
                .IsGreaterThan(VmStackDefaults.ValueStackInitialCapacity);
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
        [AllLuaVersions]
        public async Task CoroutineYieldPassesValuesWhenYieldingIsAllowed(
            LuaCompatibilityVersion version
        )
        {
            Script script = new(version, CoreModulePresets.Complete);
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
        [AllLuaVersions]
        public async Task YieldingFromMainChunkThrowsCannotYieldMain(
            LuaCompatibilityVersion version
        )
        {
            Script script = new(version, CoreModulePresets.Complete);

            ScriptRuntimeException exception = ExpectException<ScriptRuntimeException>(() =>
                script.DoString("coroutine.yield('outside')")
            );

            await Assert
                .That(exception.Message)
                .Contains("attempt to yield from outside a coroutine");
        }

        [global::TUnit.Core.Test]
        [AllLuaVersions]
        public async Task YieldingWithClrBoundaryInsideCoroutineThrowsCannotYield(
            LuaCompatibilityVersion version
        )
        {
            Script script = new(version, CoreModulePresets.Complete);
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
            using ProcessorYieldScope yieldScope = ProcessorYieldScope.Override(
                coroutineProcessor,
                newValue: false
            );

            ScriptRuntimeException exception = ExpectException<ScriptRuntimeException>(() =>
                coroutineValue.Coroutine.Resume()
            );
            await Assert
                .That(exception.Message)
                .Contains("attempt to yield across a CLR-call boundary");
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
