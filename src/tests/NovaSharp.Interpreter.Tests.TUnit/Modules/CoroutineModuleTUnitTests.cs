#pragma warning disable CA2007
namespace NovaSharp.Interpreter.Tests.TUnit.Modules
{
    using System;
    using System.Threading;
    using System.Threading.Tasks;
    using global::TUnit.Assertions;
    using NovaSharp;
    using NovaSharp.Interpreter;
    using NovaSharp.Interpreter.DataTypes;
    using NovaSharp.Interpreter.Errors;
    using NovaSharp.Interpreter.Modules;

    public sealed class CoroutineModuleTUnitTests
    {
        [global::TUnit.Core.Test]
        public async Task RunningFromMainReturnsMainCoroutine()
        {
            Script script = CreateScript();
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
            Script script = CreateScript();
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
            Script script = CreateScript();
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
            Script script = CreateScript();
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
            Script script = CreateScript();
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
            Script script = CreateScript();
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

            InternalErrorException exception = Assert.Throws<InternalErrorException>(() =>
                script.Call(statusFunc, coroutineValue)
            );

            await Assert.That(exception.Message).Contains("Unexpected coroutine state");
        }

        [global::TUnit.Core.Test]
        public async Task WrapRequiresFunctionArgument()
        {
            Script script = CreateScript();
            DynValue wrapFunc = script.Globals.Get("coroutine").Table.Get("wrap");

            ScriptRuntimeException exception = Assert.Throws<ScriptRuntimeException>(() =>
                script.Call(wrapFunc, DynValue.NewNumber(1))
            );

            await Assert.That(exception.Message).Contains("bad argument #1 to 'wrap'");
        }

        [global::TUnit.Core.Test]
        public async Task CreateRequiresFunctionArgument()
        {
            Script script = CreateScript();
            DynValue createFunc = script.Globals.Get("coroutine").Table.Get("create");

            ScriptRuntimeException exception = Assert.Throws<ScriptRuntimeException>(() =>
                script.Call(createFunc, DynValue.NewString("oops"))
            );

            await Assert.That(exception.Message).Contains("bad argument #1 to 'create'");
        }

        [global::TUnit.Core.Test]
        public async Task WrapReturnsFunctionThatResumesCoroutine()
        {
            Script script = CreateScript();
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
            Script script = CreateScript();
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
            Script script = CreateScript();
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
            Script script = CreateScript();
            DynValue resumeFunc = script.Globals.Get("coroutine").Table.Get("resume");

            ScriptRuntimeException exception = Assert.Throws<ScriptRuntimeException>(() =>
                script.Call(resumeFunc, DynValue.NewString("oops"))
            );

            await Assert.That(exception.Message).Contains("bad argument #1 to 'resume'");
        }

        [global::TUnit.Core.Test]
        public async Task ResumeFlattensNestedTupleResults()
        {
            Script script = CreateScript();
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
            Script script = CreateScript();
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

        [global::TUnit.Core.Test]
        public async Task ResumeForwardsArgumentsToCoroutine()
        {
            Script script = CreateScript();
            script.DoString(
                @"
                function sum(...)
                    local total = 0
                    for i = 1, select('#', ...) do
                        total = total + select(i, ...)
                    end
                    return total
                end
            "
            );

            DynValue resumeFunc = script.Globals.Get("coroutine").Table.Get("resume");
            DynValue coroutineValue = script.CreateCoroutine(script.Globals.Get("sum"));

            DynValue result = script.Call(
                resumeFunc,
                coroutineValue,
                DynValue.NewNumber(2),
                DynValue.NewNumber(3),
                DynValue.NewNumber(5)
            );

            await Assert.That(result.Type).IsEqualTo(DataType.Tuple);
            await Assert.That(result.Tuple.Length).IsEqualTo(2);
            await Assert.That(result.Tuple[0].Boolean).IsTrue();
            await Assert.That(result.Tuple[1].Number).IsEqualTo(10d);
        }

        [global::TUnit.Core.Test]
        public async Task ResumeFlattensTrailingTupleResults()
        {
            Script script = CreateScript();
            script.Globals["buildNestedResult"] = DynValue.NewCallback(
                (_, _) =>
                {
                    DynValue nested = DynValue.NewTuple(
                        DynValue.NewString("inner-value"),
                        DynValue.NewNumber(99)
                    );
                    return DynValue.NewTuple(DynValue.NewString("outer"), nested);
                }
            );

            script.DoString(
                @"
                function invokeNestedBuilder()
                    return buildNestedResult()
                end
            "
            );

            DynValue resumeFunc = script.Globals.Get("coroutine").Table.Get("resume");
            DynValue coroutineValue = script.CreateCoroutine(
                script.Globals.Get("invokeNestedBuilder")
            );

            DynValue result = script.Call(resumeFunc, coroutineValue);

            await Assert.That(result.Type).IsEqualTo(DataType.Tuple);
            await Assert.That(result.Tuple.Length).IsEqualTo(4);
            await Assert.That(result.Tuple[0].Boolean).IsTrue();
            await Assert.That(result.Tuple[1].String).IsEqualTo("outer");
            await Assert.That(result.Tuple[2].String).IsEqualTo("inner-value");
            await Assert.That(result.Tuple[3].Number).IsEqualTo(99d);
        }

        [global::TUnit.Core.Test]
        public async Task WrapForwardsArgumentsToCoroutineFunction()
        {
            Script script = CreateScript();
            script.DoString(
                @"
                function buildConcatWrapper()
                    return coroutine.wrap(function(a, b, c)
                        return table.concat({a, b, c}, '-')
                    end)
                end
            "
            );

            DynValue wrapper = script.Call(script.Globals.Get("buildConcatWrapper"));
            DynValue result = script.Call(
                wrapper,
                DynValue.NewString("alpha"),
                DynValue.NewString("beta"),
                DynValue.NewString("gamma")
            );

            await Assert.That(result.String).IsEqualTo("alpha-beta-gamma");
        }

        [global::TUnit.Core.Test]
        public async Task IsYieldableReturnsFalseOnMainCoroutine()
        {
            Script script = CreateScript();
            DynValue isYieldableFunc = script.Globals.Get("coroutine").Table.Get("isyieldable");

            DynValue result = script.Call(isYieldableFunc);

            await Assert.That(result.Type).IsEqualTo(DataType.Boolean);
            await Assert.That(result.Boolean).IsFalse();
        }

        [global::TUnit.Core.Test]
        public async Task IsYieldableReturnsTrueInsideCoroutine()
        {
            Script script = CreateScript();
            script.DoString(
                @"
                function buildYieldableChecker()
                    return coroutine.wrap(function()
                        return coroutine.isyieldable()
                    end)
                end
            "
            );

            DynValue wrapperBuilder = script.Globals.Get("buildYieldableChecker");
            DynValue wrapper = script.Call(wrapperBuilder);
            DynValue result = script.Call(wrapper);

            await Assert.That(result.Type).IsEqualTo(DataType.Boolean);
            await Assert.That(result.Boolean).IsTrue();
        }

        [global::TUnit.Core.Test]
        public async Task WrapPropagatesErrorsToCaller()
        {
            Script script = CreateScript();
            script.DoString(
                @"
                function buildErrorWrapper()
                    return coroutine.wrap(function()
                        error('wrap boom', 0)
                    end)
                end
            "
            );

            DynValue wrapper = script.Call(script.Globals.Get("buildErrorWrapper"));

            ScriptRuntimeException exception = Assert.Throws<ScriptRuntimeException>(() =>
                script.Call(wrapper)
            );

            await Assert.That(exception.Message).Contains("wrap boom");
        }

        [global::TUnit.Core.Test]
        public async Task WrapPropagatesErrorsAfterYield()
        {
            Script script = CreateScript();
            script.DoString(
                @"
                function buildDelayedErrorWrapper()
                    return coroutine.wrap(function()
                        coroutine.yield('first')
                        error('wrap later', 0)
                    end)
                end
            "
            );

            DynValue wrapper = script.Call(script.Globals.Get("buildDelayedErrorWrapper"));
            DynValue first = script.Call(wrapper);

            await Assert.That(first.Type).IsEqualTo(DataType.String);
            await Assert.That(first.String).IsEqualTo("first");

            ScriptRuntimeException exception = Assert.Throws<ScriptRuntimeException>(() =>
                script.Call(wrapper)
            );

            await Assert.That(exception.Message).Contains("wrap later");
        }

        [global::TUnit.Core.Test]
        public async Task ResumeForceSuspendedCoroutineRejectsArguments()
        {
            Script script = CreateScript();
            script.DoString(
                @"
                function heavy()
                    local sum = 0
                    for i = 1, 200 do
                        sum = sum + i
                    end
                    return sum
                end
            "
            );

            DynValue resumeFunc = script.Globals.Get("coroutine").Table.Get("resume");
            DynValue coroutineValue = script.CreateCoroutine(script.Globals.Get("heavy"));
            coroutineValue.Coroutine.AutoYieldCounter = 1;

            DynValue first = script.Call(resumeFunc, coroutineValue);
            await Assert.That(first.Type).IsEqualTo(DataType.Tuple);
            await Assert.That(first.Tuple[0].Boolean).IsTrue();
            await Assert.That(first.Tuple[1].Type).IsEqualTo(DataType.YieldRequest);
            await Assert.That(first.Tuple[1].YieldRequest.Forced).IsTrue();

            ArgumentException exception = Assert.Throws<ArgumentException>(() =>
                script.Call(resumeFunc, coroutineValue, DynValue.NewNumber(42))
            );

            await Assert.That(exception.Message).Contains("args must be empty");
        }

        [global::TUnit.Core.Test]
        public async Task IsYieldableReturnsFalseInsideClrCallback()
        {
            Script script = CreateScript();
            DynValue isYieldableFunc = script.Globals.Get("coroutine").Table.Get("isyieldable");

            script.Globals["clrCheck"] = DynValue.NewCallback(
                (context, _) =>
                {
                    DynValue value = context.Call(isYieldableFunc);
                    return value;
                }
            );

            script.DoString(
                @"
                function invokeClrCheck()
                    return clrCheck()
                end
            "
            );

            DynValue result = script.Call(script.Globals.Get("invokeClrCheck"));

            await Assert.That(result.Type).IsEqualTo(DataType.Boolean);
            await Assert.That(result.Boolean).IsFalse();
        }

        [global::TUnit.Core.Test]
        public async Task WrapWithPcallCapturesErrors()
        {
            Script script = CreateScript();
            script.DoString(
                @"
                function buildPcallWrapper()
                    return coroutine.wrap(function()
                        error('wrapped failure', 0)
                    end)
                end
            "
            );

            DynValue wrapper = script.Call(script.Globals.Get("buildPcallWrapper"));
            DynValue pcall = script.Globals.Get("pcall");
            DynValue result = script.Call(pcall, wrapper);

            await Assert.That(result.Type).IsEqualTo(DataType.Tuple);
            await Assert.That(result.Tuple.Length).IsGreaterThanOrEqualTo(2);
            await Assert.That(result.Tuple[0].Boolean).IsFalse();
            await Assert.That(result.Tuple[1].String).Contains("wrapped failure");
        }

        [global::TUnit.Core.Test]
        public async Task WrapWithPcallReturnsYieldedValues()
        {
            Script script = CreateScript();
            script.DoString(
                @"
                function buildYieldingWrapper()
                    local step = 0
                    return coroutine.wrap(function()
                        step = step + 1
                        coroutine.yield('first')
                        return 'complete', step
                    end)
                end
            "
            );

            DynValue wrapper = script.Call(script.Globals.Get("buildYieldingWrapper"));
            DynValue pcall = script.Globals.Get("pcall");

            DynValue first = script.Call(pcall, wrapper);
            await Assert.That(first.Tuple[0].Boolean).IsTrue();
            await Assert.That(first.Tuple[1].String).IsEqualTo("first");

            DynValue second = script.Call(pcall, wrapper);
            await Assert.That(second.Tuple[0].Boolean).IsTrue();
            await Assert.That(second.Tuple[1].String).IsEqualTo("complete");
            await Assert.That(second.Tuple[2].Number).IsEqualTo(1d);
        }

        [global::TUnit.Core.Test]
        public async Task IsYieldableInsidePcallWithinCoroutine()
        {
            Script script = CreateScript();
            script.DoString(
                @"
                function pcallyield()
                    return coroutine.isyieldable()
                end

                function run_pcall_inside_coroutine()
                    local ok, value = pcall(pcallyield)
                    return ok, value
                end
            "
            );

            DynValue coroutineValue = script.CreateCoroutine(
                script.Globals.Get("run_pcall_inside_coroutine")
            );
            DynValue result = coroutineValue.Coroutine.Resume();

            await Assert.That(result.Type).IsEqualTo(DataType.Tuple);
            await Assert.That(result.Tuple[0].Boolean).IsTrue();
            await Assert.That(result.Tuple[1].Type).IsEqualTo(DataType.Boolean);
            await Assert.That(result.Tuple[1].Boolean).IsTrue();
        }

        [global::TUnit.Core.Test]
        public async Task WrapWithPcallHandlesTailCalls()
        {
            Script script = CreateScript();
            script.DoString(
                @"
                function tail_target(...)
                    return 'tail', ...
                end

                function build_tail_wrapper()
                    return coroutine.wrap(function(...)
                        return tail_target(...)
                    end)
                end
            "
            );

            DynValue wrapper = script.Call(script.Globals.Get("build_tail_wrapper"));
            DynValue pcall = script.Globals.Get("pcall");

            DynValue result = script.Call(
                pcall,
                wrapper,
                DynValue.NewString("alpha"),
                DynValue.NewNumber(42)
            );

            await Assert.That(result.Type).IsEqualTo(DataType.Tuple);
            await Assert.That(result.Tuple[0].Boolean).IsTrue();
            await Assert.That(result.Tuple[1].String).IsEqualTo("tail");
            await Assert.That(result.Tuple[2].String).IsEqualTo("alpha");
            await Assert.That(result.Tuple[3].Number).IsEqualTo(42d);
        }

        [global::TUnit.Core.Test]
        public async Task IsYieldableInsideXpcallErrorHandlerWithinCoroutine()
        {
            Script script = CreateScript();
            script.DoString(
                @"
                handlerYieldable = nil

                function error_handler(err)
                    handlerYieldable = coroutine.isyieldable()
                    return err
                end

                function run_xpcall_inside_coroutine()
                    return xpcall(function()
                        error('boom', 0)
                    end, error_handler)
                end
            "
            );

            DynValue statusFunc = script.Globals.Get("coroutine").Table.Get("status");
            DynValue coroutineValue = script.CreateCoroutine(
                script.Globals.Get("run_xpcall_inside_coroutine")
            );

            DynValue result = coroutineValue.Coroutine.Resume();
            await Assert.That(result.Type).IsEqualTo(DataType.Tuple);
            await Assert.That(result.Tuple[0].Boolean).IsFalse();
            await Assert.That(result.Tuple[1].String).Contains("boom");

            DynValue status = script.Call(statusFunc, coroutineValue);
            await Assert.That(status.String).IsEqualTo("dead");

            DynValue handlerYieldable = script.Globals.Get("handlerYieldable");
            await Assert.That(handlerYieldable.Type).IsEqualTo(DataType.Boolean);
            await Assert.That(handlerYieldable.Boolean).IsFalse();
        }

        [global::TUnit.Core.Test]
        public async Task CoroutineStatusRemainsAccurateAfterNestedResumes()
        {
            Script script = CreateScript();
            script.DoString(
                @"
                loggedStatuses = {}

                function outerCoroutine()
                    local inner = coroutine.create(function()
                        coroutine.yield('inner-yield')
                        return 'inner-done'
                    end)

                    local ok, value = coroutine.resume(inner)
                    table.insert(loggedStatuses, coroutine.status(inner))
                    coroutine.yield('outer-yield')
                    table.insert(loggedStatuses, coroutine.status(inner))
                    ok, value = coroutine.resume(inner)
                    table.insert(loggedStatuses, coroutine.status(inner))
                    return value
                end
            "
            );

            DynValue statusFunc = script.Globals.Get("coroutine").Table.Get("status");
            DynValue coroutineValue = script.CreateCoroutine(script.Globals.Get("outerCoroutine"));

            DynValue firstYield = coroutineValue.Coroutine.Resume();
            await Assert.That(firstYield.Type).IsEqualTo(DataType.String);
            await Assert.That(firstYield.String).IsEqualTo("outer-yield");

            DynValue outerStatusAfterYield = script.Call(statusFunc, coroutineValue);
            await Assert.That(outerStatusAfterYield.String).IsEqualTo("suspended");

            DynValue secondResume = coroutineValue.Coroutine.Resume();
            await Assert.That(secondResume.Type).IsEqualTo(DataType.String);
            await Assert.That(secondResume.String).IsEqualTo("inner-done");

            DynValue outerStatusAfterCompletion = script.Call(statusFunc, coroutineValue);
            await Assert.That(outerStatusAfterCompletion.String).IsEqualTo("dead");

            DynValue loggedStatuses = script.Globals.Get("loggedStatuses");
            await Assert.That(loggedStatuses.Type).IsEqualTo(DataType.Table);
            await Assert.That(loggedStatuses.Table.Length).IsEqualTo(3);
            await Assert.That(loggedStatuses.Table.Get(1).String).IsEqualTo("suspended");
            await Assert.That(loggedStatuses.Table.Get(2).String).IsEqualTo("suspended");
            await Assert.That(loggedStatuses.Table.Get(3).String).IsEqualTo("dead");
        }

        [global::TUnit.Core.Test]
        public async Task ResumeFromDifferentThreadThrowsInvalidOperation()
        {
            Script script = CreateScript();
            using ManualResetEventSlim entered = new(false);
            using ManualResetEventSlim allowCompletion = new(false);

            script.Globals["waitForSignal"] = DynValue.NewCallback(
                (_, _) =>
                {
                    entered.Set();
                    allowCompletion.Wait();
                    return DynValue.Nil;
                }
            );

            script.DoString(
                @"
                function pause()
                    waitForSignal()
                    return 'done'
                end
            "
            );

            DynValue resumeFunc = script.Globals.Get("coroutine").Table.Get("resume");
            DynValue coroutineValue = script.CreateCoroutine(script.Globals.Get("pause"));

            Task<DynValue> background = Task.Run(() => script.Call(resumeFunc, coroutineValue));
            try
            {
                await Assert.That(entered.Wait(TimeSpan.FromSeconds(2))).IsTrue();

                InvalidOperationException exception = Assert.Throws<InvalidOperationException>(() =>
                    script.Call(resumeFunc, coroutineValue)
                );

                await Assert
                    .That(exception.Message)
                    .Contains("Cannot enter the same NovaSharp processor");
            }
            finally
            {
                allowCompletion.Set();
            }

            DynValue result = await background;
            await Assert.That(result.Type).IsEqualTo(DataType.Tuple);
            await Assert.That(result.Tuple[0].Boolean).IsTrue();
            await Assert.That(result.Tuple[1].String).IsEqualTo("done");
        }

        [global::TUnit.Core.Test]
        public async Task ResumeDeadCoroutineReturnsErrorTuple()
        {
            Script script = CreateScript();
            script.DoString(
                @"
                function finish()
                    return 'completed'
                end
            "
            );

            DynValue resumeFunc = script.Globals.Get("coroutine").Table.Get("resume");
            DynValue coroutineValue = script.CreateCoroutine(script.Globals.Get("finish"));

            DynValue first = script.Call(resumeFunc, coroutineValue);
            await Assert.That(first.Tuple[0].Boolean).IsTrue();

            DynValue result = script.Call(resumeFunc, coroutineValue);
            await Assert.That(result.Type).IsEqualTo(DataType.Tuple);
            await Assert.That(result.Tuple[0].Boolean).IsFalse();
            await Assert.That(result.Tuple[1].String).Contains("cannot resume dead coroutine");
        }

        [global::TUnit.Core.Test]
        public async Task YieldReturnsYieldRequestWithArguments()
        {
            Script script = CreateScript();
            DynValue yieldFunc = script.Globals.Get("coroutine").Table.Get("yield");

            DynValue result = script.Call(
                yieldFunc,
                DynValue.NewNumber(7),
                DynValue.NewString("value")
            );

            await Assert.That(result.Type).IsEqualTo(DataType.YieldRequest);
            await Assert.That(result.YieldRequest.Forced).IsFalse();

            ReadOnlyMemory<DynValue> values = result.YieldRequest.ReturnValues;
            await Assert.That(values.Length).IsEqualTo(2);
            await Assert.That(values.Span[0].Number).IsEqualTo(7d);
            await Assert.That(values.Span[1].String).IsEqualTo("value");
        }

        private static Script CreateScript()
        {
            return new Script(CoreModules.PresetComplete);
        }
    }
}
#pragma warning restore CA2007
