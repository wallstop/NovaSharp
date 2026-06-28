namespace WallstopStudios.NovaSharp.Interpreter.Tests.TUnit.Units.Execution.ProcessorExecution
{
    using System;
    using System.Collections.Generic;
    using System.Threading.Tasks;
    using global::TUnit.Assertions;
    using NovaSharp;
    using WallstopStudios.NovaSharp.Interpreter;
    using WallstopStudios.NovaSharp.Interpreter.Compatibility;
    using WallstopStudios.NovaSharp.Interpreter.DataTypes;
    using WallstopStudios.NovaSharp.Interpreter.Errors;
    using WallstopStudios.NovaSharp.Interpreter.Execution;
    using WallstopStudios.NovaSharp.Interpreter.Execution.VM;
    using WallstopStudios.NovaSharp.Interpreter.Modules;
    using WallstopStudios.NovaSharp.Tests.TestInfrastructure.Scopes;
    using WallstopStudios.NovaSharp.Tests.TestInfrastructure.TUnit;

    public sealed class ProcessorCoroutineLifecycleTUnitTests
    {
        [global::TUnit.Core.Test]
        [AllLuaVersions]
        public async Task ResumeAfterCompletionThrowsCannotResumeNotSuspended(
            LuaCompatibilityVersion version
        )
        {
            Script script = new(version, CoreModulePresets.Complete);
            script.DoString("function simple() return 5 end");

            DynValue coroutineValue = script.CreateCoroutine(script.Globals.Get("simple"));

            DynValue first = coroutineValue.Coroutine.Resume();
            await Assert.That(first.Number).IsEqualTo(5d);
            await Assert.That(coroutineValue.Coroutine.State).IsEqualTo(CoroutineState.Dead);

            ScriptRuntimeException exception = ExpectException<ScriptRuntimeException>(() =>
                coroutineValue.Coroutine.Resume()
            );
            await Assert.That(exception.Message).Contains("cannot resume dead coroutine");
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

        private static TException ExpectException<TException>(System.Action action)
            where TException : System.Exception
        {
            try
            {
                action();
            }
            catch (TException ex)
            {
                return ex;
            }

            throw new System.InvalidOperationException(
                $"Expected exception of type {typeof(TException).Name}."
            );
        }
    }
}
