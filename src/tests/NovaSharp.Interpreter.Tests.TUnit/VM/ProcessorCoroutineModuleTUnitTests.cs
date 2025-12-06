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
    using NovaSharp.Interpreter.Errors;
    using NovaSharp.Interpreter.Modules;

    public sealed class ProcessorCoroutineModuleTUnitTests
    {
        [global::TUnit.Core.Test]
        public async Task RunningFromMainReturnsMainCoroutine()
        {
            Script script = new(CoreModules.PresetComplete);
            DynValue runningFunc = script.Globals.Get("coroutine").Table.Get("running");

            DynValue result = script.Call(runningFunc);
            await Assert.That(result.Type).IsEqualTo(DataType.Tuple);
            await Assert.That(result.Tuple.Length).IsGreaterThanOrEqualTo(2);

            DynValue coroutineValue = result.Tuple[0];
            DynValue isMain = result.Tuple[1];

            await Assert.That(coroutineValue.Type).IsEqualTo(DataType.Thread);
            await Assert.That(coroutineValue.Coroutine.State).IsEqualTo(CoroutineState.Main);
            await Assert.That(isMain.Type).IsEqualTo(DataType.Boolean);
            await Assert.That(isMain.Boolean).IsTrue();
        }

        [global::TUnit.Core.Test]
        public async Task RunningInsideCoroutineReturnsFalse()
        {
            Script script = new(CoreModules.PresetComplete);
            script.DoString(
                @"
                function runningCheck()
                    local _, isMain = coroutine.running()
                    return isMain
                end
            "
            );

            DynValue coroutineValue = script.CreateCoroutine(script.Globals.Get("runningCheck"));
            DynValue result = coroutineValue.Coroutine.Resume();

            await Assert.That(result.Type).IsEqualTo(DataType.Boolean);
            await Assert.That(result.Boolean).IsFalse();
        }

        [global::TUnit.Core.Test]
        public async Task StatusReflectsLifecycleAndForceSuspendedStates()
        {
            Script script = new(CoreModules.PresetComplete);
            script.DoString(
                @"
                function compute()
                    local sum = 0
                    for i = 1, 200 do
                        sum = sum + i
                    end
                    return sum
                end
            "
            );

            DynValue statusFunc = script.Globals.Get("coroutine").Table.Get("status");
            DynValue coroutineValue = script.CreateCoroutine(script.Globals.Get("compute"));

            DynValue initialStatus = script.Call(statusFunc, coroutineValue);
            await Assert.That(initialStatus.String).IsEqualTo("suspended");

            coroutineValue.Coroutine.AutoYieldCounter = 1;
            DynValue forced = coroutineValue.Coroutine.Resume();
            await Assert.That(forced.Type).IsEqualTo(DataType.YieldRequest);
            await Assert.That(forced.YieldRequest.Forced).IsTrue();

            DynValue suspendedStatus = script.Call(statusFunc, coroutineValue);
            await Assert.That(suspendedStatus.String).IsEqualTo("suspended");

            coroutineValue.Coroutine.AutoYieldCounter = 0;
            DynValue final = coroutineValue.Coroutine.Resume();
            await Assert.That(final.Type).IsEqualTo(DataType.Number);

            DynValue deadStatus = script.Call(statusFunc, coroutineValue);
            await Assert.That(deadStatus.String).IsEqualTo("dead");
        }

        [global::TUnit.Core.Test]
        public async Task StatusReturnsRunningForActiveCoroutine()
        {
            Script script = new(CoreModules.PresetComplete);
            script.DoString(
                @"
                function queryRunningStatus()
                    local current = select(1, coroutine.running())
                    return coroutine.status(current)
                end
            "
            );

            DynValue coroutineValue = script.CreateCoroutine(
                script.Globals.Get("queryRunningStatus")
            );
            DynValue result = coroutineValue.Coroutine.Resume();

            await Assert.That(result.Type).IsEqualTo(DataType.String);
            await Assert.That(result.String).IsEqualTo("running");
        }

        [global::TUnit.Core.Test]
        public async Task StatusReturnsNormalWhenInspectingMainFromChild()
        {
            Script script = new(CoreModules.PresetComplete);
            script.DoString(
                @"
                local mainCoroutine = select(1, coroutine.running())

                function queryMainStatus()
                    return coroutine.status(mainCoroutine)
                end
            "
            );

            DynValue coroutineValue = script.CreateCoroutine(script.Globals.Get("queryMainStatus"));
            DynValue result = coroutineValue.Coroutine.Resume();

            await Assert.That(result.Type).IsEqualTo(DataType.String);
            await Assert.That(result.String).IsEqualTo("normal");
        }

        [global::TUnit.Core.Test]
        public async Task StatusThrowsForUnknownStates()
        {
            Script script = new(CoreModules.PresetComplete);
            script.DoString(
                @"
                function idle()
                    return 1
                end
            "
            );

            DynValue statusFunc = script.Globals.Get("coroutine").Table.Get("status");
            DynValue coroutineValue = script.CreateCoroutine(script.Globals.Get("idle"));

            coroutineValue.Coroutine.ForceStateForTests((CoroutineState)0);

            InternalErrorException exception = ExpectException<InternalErrorException>(() =>
                script.Call(statusFunc, coroutineValue)
            );
            await Assert.That(exception.Message).Contains("Unexpected coroutine state");
        }

        [global::TUnit.Core.Test]
        public async Task WrapRequiresFunctionArgument()
        {
            Script script = new(CoreModules.PresetComplete);
            DynValue wrapFunc = script.Globals.Get("coroutine").Table.Get("wrap");

            ScriptRuntimeException exception = ExpectException<ScriptRuntimeException>(() =>
                script.Call(wrapFunc, DynValue.NewNumber(1))
            );
            await Assert.That(exception.Message).Contains("bad argument #1 to 'wrap'");
        }

        [global::TUnit.Core.Test]
        public async Task CreateRequiresFunctionArgument()
        {
            Script script = new(CoreModules.PresetComplete);
            DynValue createFunc = script.Globals.Get("coroutine").Table.Get("create");

            ScriptRuntimeException exception = ExpectException<ScriptRuntimeException>(() =>
                script.Call(createFunc, DynValue.NewString("oops"))
            );
            await Assert.That(exception.Message).Contains("bad argument #1 to 'create'");
        }

        [global::TUnit.Core.Test]
        public async Task WrapReturnsFunctionThatResumesCoroutine()
        {
            Script script = new(CoreModules.PresetComplete);
            script.DoString(
                @"
                function buildWrapper()
                    return coroutine.wrap(function()
                        for i = 1, 3 do
                            coroutine.yield(i)
                        end
                        return 'done'
                    end)
                end
            "
            );

            DynValue wrapper = script.Call(script.Globals.Get("buildWrapper"));
            await Assert.That(wrapper.Type).IsEqualTo(DataType.ClrFunction);

            DynValue first = script.Call(wrapper);
            DynValue second = script.Call(wrapper);
            DynValue third = script.Call(wrapper);
            DynValue final = script.Call(wrapper);

            await Assert.That(first.Number).IsEqualTo(1d);
            await Assert.That(second.Number).IsEqualTo(2d);
            await Assert.That(third.Number).IsEqualTo(3d);
            await Assert.That(final.String).IsEqualTo("done");
        }

        [global::TUnit.Core.Test]
        public async Task ResumeFlattensResultsAndReportsSuccess()
        {
            Script script = new(CoreModules.PresetComplete);
            script.DoString(
                @"
                function generator()
                    coroutine.yield('yielded', 42)
                    return 7, 8
                end
            "
            );

            DynValue resumeFunc = script.Globals.Get("coroutine").Table.Get("resume");
            DynValue coroutineValue = script.CreateCoroutine(script.Globals.Get("generator"));

            DynValue first = script.Call(resumeFunc, coroutineValue);
            await Assert.That(first.Type).IsEqualTo(DataType.Tuple);
            await Assert.That(first.Tuple.Length).IsEqualTo(3);
            await Assert.That(first.Tuple[0].Boolean).IsTrue();
            await Assert.That(first.Tuple[1].String).IsEqualTo("yielded");
            await Assert.That(first.Tuple[2].Number).IsEqualTo(42d);

            DynValue second = script.Call(resumeFunc, coroutineValue);
            await Assert.That(second.Tuple[0].Boolean).IsTrue();
            await Assert.That(second.Tuple[1].Number).IsEqualTo(7d);
            await Assert.That(second.Tuple[2].Number).IsEqualTo(8d);
        }

        [global::TUnit.Core.Test]
        public async Task ResumeReportsErrorsAsFalseWithMessage()
        {
            Script script = new(CoreModules.PresetComplete);
            script.DoString(
                @"
                function explode()
                    error('boom', 0)
                end
            "
            );

            DynValue resumeFunc = script.Globals.Get("coroutine").Table.Get("resume");
            DynValue coroutineValue = script.CreateCoroutine(script.Globals.Get("explode"));

            DynValue result = script.Call(resumeFunc, coroutineValue);
            await Assert.That(result.Type).IsEqualTo(DataType.Tuple);
            await Assert.That(result.Tuple[0].Boolean).IsFalse();
            await Assert.That(result.Tuple[1].String).Contains("boom");
        }

        [global::TUnit.Core.Test]
        public async Task ResumeRequiresThreadArgument()
        {
            Script script = new(CoreModules.PresetComplete);
            DynValue resumeFunc = script.Globals.Get("coroutine").Table.Get("resume");

            ScriptRuntimeException exception = ExpectException<ScriptRuntimeException>(() =>
                script.Call(resumeFunc, DynValue.NewString("oops"))
            );
            await Assert.That(exception.Message).Contains("bad argument #1 to 'resume'");
        }

        [global::TUnit.Core.Test]
        public async Task ResumeFlattensNestedTupleResults()
        {
            Script script = new(CoreModules.PresetComplete);
            script.DoString(
                @"
                function returningTuple()
                    return 'tag', coroutine.running()
                end
            "
            );

            DynValue resumeFunc = script.Globals.Get("coroutine").Table.Get("resume");
            DynValue coroutineValue = script.CreateCoroutine(script.Globals.Get("returningTuple"));

            DynValue result = script.Call(resumeFunc, coroutineValue);

            await Assert.That(result.Type).IsEqualTo(DataType.Tuple);
            await Assert.That(result.Tuple.Length).IsEqualTo(4);
            await Assert.That(result.Tuple[0].Boolean).IsTrue();
            await Assert.That(result.Tuple[1].String).IsEqualTo("tag");
            await Assert.That(result.Tuple[2].Type).IsEqualTo(DataType.Thread);
            await Assert.That(result.Tuple[3].Boolean).IsFalse();
        }

        [global::TUnit.Core.Test]
        public async Task ResumeDeeplyNestedTuplesAreFlattened()
        {
            Script script = new(CoreModules.PresetComplete);
            script.DoString(
                @"
                function buildDeepCoroutine()
                    local function deepest()
                        return 'deep', 'value'
                    end

                    local function middle()
                        return coroutine.resume(coroutine.create(deepest))
                    end

                    local function top()
                        return 'top', coroutine.resume(coroutine.create(middle))
                    end

                    return coroutine.create(top)
                end
            "
            );

            DynValue resumeFunc = script.Globals.Get("coroutine").Table.Get("resume");
            DynValue coroutineValue = script.Call(script.Globals.Get("buildDeepCoroutine"));

            DynValue result = script.Call(resumeFunc, coroutineValue);

            await Assert.That(result.Type).IsEqualTo(DataType.Tuple);
            await Assert.That(result.Tuple.Length).IsEqualTo(6);
            await Assert.That(result.Tuple[0].Boolean).IsTrue();
            await Assert.That(result.Tuple[1].String).IsEqualTo("top");
            await Assert.That(result.Tuple[2].Boolean).IsTrue();
            await Assert.That(result.Tuple[3].Boolean).IsTrue();
            await Assert.That(result.Tuple[4].String).IsEqualTo("deep");
            await Assert.That(result.Tuple[5].String).IsEqualTo("value");
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
