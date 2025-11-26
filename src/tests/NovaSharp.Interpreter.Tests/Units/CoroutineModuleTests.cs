namespace NovaSharp.Interpreter.Tests.Units
{
    using System;
    using System.Threading;
    using System.Threading.Tasks;
    using NovaSharp;
    using NovaSharp.Interpreter;
    using NovaSharp.Interpreter.DataTypes;
    using NovaSharp.Interpreter.Errors;
    using NovaSharp.Interpreter.Modules;
    using NUnit.Framework;

    [TestFixture]
    public sealed class CoroutineModuleTests
    {
        [Test]
        public void RunningFromMainReturnsMainCoroutine()
        {
            Script script = new(CoreModules.PresetComplete);
            DynValue runningFunc = script.Globals.Get("coroutine").Table.Get("running");

            DynValue result = script.Call(runningFunc);
            Assert.That(result.Type, Is.EqualTo(DataType.Tuple));
            Assert.That(result.Tuple.Length, Is.GreaterThanOrEqualTo(2));

            DynValue coroutineValue = result.Tuple[0];
            DynValue isMain = result.Tuple[1];

            Assert.That(coroutineValue.Type, Is.EqualTo(DataType.Thread));
            Assert.That(coroutineValue.Coroutine.State, Is.EqualTo(CoroutineState.Main));
            Assert.That(isMain.Type, Is.EqualTo(DataType.Boolean));
            Assert.That(isMain.Boolean, Is.True);
        }

        [Test]
        public void RunningInsideCoroutineReturnsFalse()
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

            Assert.That(result.Type, Is.EqualTo(DataType.Boolean));
            Assert.That(result.Boolean, Is.False);
        }

        [Test]
        public void StatusReflectsLifecycleAndForceSuspendedStates()
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
            Assert.That(initialStatus.String, Is.EqualTo("suspended"));

            coroutineValue.Coroutine.AutoYieldCounter = 1;
            DynValue forced = coroutineValue.Coroutine.Resume();
            Assert.That(forced.Type, Is.EqualTo(DataType.YieldRequest));
            Assert.That(forced.YieldRequest.Forced, Is.True);

            DynValue suspendedStatus = script.Call(statusFunc, coroutineValue);
            Assert.That(suspendedStatus.String, Is.EqualTo("suspended"));

            coroutineValue.Coroutine.AutoYieldCounter = 0;
            DynValue final = coroutineValue.Coroutine.Resume();
            Assert.That(final.Type, Is.EqualTo(DataType.Number));

            DynValue deadStatus = script.Call(statusFunc, coroutineValue);
            Assert.That(deadStatus.String, Is.EqualTo("dead"));
        }

        [Test]
        public void StatusReturnsRunningForActiveCoroutine()
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

            Assert.That(result.Type, Is.EqualTo(DataType.String));
            Assert.That(result.String, Is.EqualTo("running"));
        }

        [Test]
        public void StatusReturnsNormalWhenInspectingMainFromChild()
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

            Assert.That(result.Type, Is.EqualTo(DataType.String));
            Assert.That(result.String, Is.EqualTo("normal"));
        }

        [Test]
        public void StatusThrowsForUnknownStates()
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

            Assert.That(
                () => script.Call(statusFunc, coroutineValue),
                Throws
                    .TypeOf<InternalErrorException>()
                    .With.Message.Contains("Unexpected coroutine state")
            );
        }

        [Test]
        public void WrapRequiresFunctionArgument()
        {
            Script script = new(CoreModules.PresetComplete);
            DynValue wrapFunc = script.Globals.Get("coroutine").Table.Get("wrap");

            Assert.That(
                () => script.Call(wrapFunc, DynValue.NewNumber(1)),
                Throws
                    .TypeOf<ScriptRuntimeException>()
                    .With.Message.Contains("bad argument #1 to 'wrap'")
            );
        }

        [Test]
        public void CreateRequiresFunctionArgument()
        {
            Script script = new(CoreModules.PresetComplete);
            DynValue createFunc = script.Globals.Get("coroutine").Table.Get("create");

            Assert.That(
                () => script.Call(createFunc, DynValue.NewString("oops")),
                Throws
                    .TypeOf<ScriptRuntimeException>()
                    .With.Message.Contains("bad argument #1 to 'create'")
            );
        }

        [Test]
        public void WrapReturnsFunctionThatResumesCoroutine()
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
            Assert.That(wrapper.Type, Is.EqualTo(DataType.ClrFunction));

            DynValue first = script.Call(wrapper);
            DynValue second = script.Call(wrapper);
            DynValue third = script.Call(wrapper);
            DynValue final = script.Call(wrapper);

            Assert.Multiple(() =>
            {
                Assert.That(first.Number, Is.EqualTo(1));
                Assert.That(second.Number, Is.EqualTo(2));
                Assert.That(third.Number, Is.EqualTo(3));
                Assert.That(final.String, Is.EqualTo("done"));
            });
        }

        [Test]
        public void ResumeFlattensResultsAndReportsSuccess()
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
            Assert.Multiple(() =>
            {
                Assert.That(first.Type, Is.EqualTo(DataType.Tuple));
                Assert.That(first.Tuple.Length, Is.EqualTo(3));
                Assert.That(first.Tuple[0].Boolean, Is.True);
                Assert.That(first.Tuple[1].String, Is.EqualTo("yielded"));
                Assert.That(first.Tuple[2].Number, Is.EqualTo(42));
            });

            DynValue second = script.Call(resumeFunc, coroutineValue);
            Assert.Multiple(() =>
            {
                Assert.That(second.Tuple[0].Boolean, Is.True);
                Assert.That(second.Tuple[1].Number, Is.EqualTo(7));
                Assert.That(second.Tuple[2].Number, Is.EqualTo(8));
            });
        }

        [Test]
        public void ResumeReportsErrorsAsFalseWithMessage()
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
            Assert.Multiple(() =>
            {
                Assert.That(result.Type, Is.EqualTo(DataType.Tuple));
                Assert.That(result.Tuple[0].Boolean, Is.False);
                Assert.That(result.Tuple[1].String, Does.Contain("boom"));
            });
        }

        [Test]
        public void ResumeRequiresThreadArgument()
        {
            Script script = new(CoreModules.PresetComplete);
            DynValue resumeFunc = script.Globals.Get("coroutine").Table.Get("resume");

            Assert.That(
                () => script.Call(resumeFunc, DynValue.NewString("oops")),
                Throws
                    .TypeOf<ScriptRuntimeException>()
                    .With.Message.Contains("bad argument #1 to 'resume'")
            );
        }

        [Test]
        public void ResumeFlattensNestedTupleResults()
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

            Assert.Multiple(() =>
            {
                Assert.That(result.Type, Is.EqualTo(DataType.Tuple));
                Assert.That(result.Tuple.Length, Is.EqualTo(4));
                Assert.That(result.Tuple[0].Boolean, Is.True);
                Assert.That(result.Tuple[1].String, Is.EqualTo("tag"));
                Assert.That(result.Tuple[2].Type, Is.EqualTo(DataType.Thread));
                Assert.That(result.Tuple[3].Boolean, Is.False);
            });
        }

        [Test]
        public void ResumeDeeplyNestedTuplesAreFlattened()
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

            Assert.Multiple(() =>
            {
                Assert.That(result.Type, Is.EqualTo(DataType.Tuple));
                Assert.That(result.Tuple.Length, Is.EqualTo(6));
                Assert.That(result.Tuple[0].Boolean, Is.True);
                Assert.That(result.Tuple[1].String, Is.EqualTo("top"));
                Assert.That(result.Tuple[2].Boolean, Is.True);
                Assert.That(result.Tuple[3].Boolean, Is.True);
                Assert.That(result.Tuple[4].String, Is.EqualTo("deep"));
                Assert.That(result.Tuple[5].String, Is.EqualTo("value"));
            });
        }

        [Test]
        public void ResumeForwardsArgumentsToCoroutine()
        {
            Script script = new(CoreModules.PresetComplete);
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

            Assert.Multiple(() =>
            {
                Assert.That(result.Type, Is.EqualTo(DataType.Tuple));
                Assert.That(result.Tuple.Length, Is.EqualTo(2));
                Assert.That(result.Tuple[0].Boolean, Is.True);
                Assert.That(result.Tuple[1].Number, Is.EqualTo(10));
            });
        }

        [Test]
        public void ResumeFlattensTrailingTupleResults()
        {
            Script script = new(CoreModules.PresetComplete);
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

            Assert.Multiple(() =>
            {
                Assert.That(result.Type, Is.EqualTo(DataType.Tuple));
                Assert.That(result.Tuple.Length, Is.EqualTo(4));
                Assert.That(result.Tuple[0].Boolean, Is.True);
                Assert.That(result.Tuple[1].String, Is.EqualTo("outer"));
                Assert.That(result.Tuple[2].String, Is.EqualTo("inner-value"));
                Assert.That(result.Tuple[3].Number, Is.EqualTo(99));
            });
        }

        [Test]
        public void WrapForwardsArgumentsToCoroutineFunction()
        {
            Script script = new(CoreModules.PresetComplete);
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

            Assert.That(result.String, Is.EqualTo("alpha-beta-gamma"));
        }

        [Test]
        public void IsYieldableReturnsFalseOnMainCoroutine()
        {
            Script script = new(CoreModules.PresetComplete);
            DynValue isYieldableFunc = script.Globals.Get("coroutine").Table.Get("isyieldable");

            DynValue result = script.Call(isYieldableFunc);

            Assert.Multiple(() =>
            {
                Assert.That(result.Type, Is.EqualTo(DataType.Boolean));
                Assert.That(result.Boolean, Is.False);
            });
        }

        [Test]
        public void IsYieldableReturnsTrueInsideCoroutine()
        {
            Script script = new(CoreModules.PresetComplete);
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

            Assert.Multiple(() =>
            {
                Assert.That(result.Type, Is.EqualTo(DataType.Boolean));
                Assert.That(result.Boolean, Is.True);
            });
        }

        [Test]
        public void WrapPropagatesErrorsToCaller()
        {
            Script script = new(CoreModules.PresetComplete);
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

            Assert.That(
                () => script.Call(wrapper),
                Throws.TypeOf<ScriptRuntimeException>().With.Message.Contains("wrap boom")
            );
        }

        [Test]
        public void WrapPropagatesErrorsAfterYield()
        {
            Script script = new(CoreModules.PresetComplete);
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

            Assert.Multiple(() =>
            {
                Assert.That(first.Type, Is.EqualTo(DataType.String));
                Assert.That(first.String, Is.EqualTo("first"));
            });

            Assert.That(
                () => script.Call(wrapper),
                Throws.TypeOf<ScriptRuntimeException>().With.Message.Contains("wrap later")
            );
        }

        [Test]
        public void ResumeForceSuspendedCoroutineRejectsArguments()
        {
            Script script = new(CoreModules.PresetComplete);
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
            Assert.Multiple(() =>
            {
                Assert.That(first.Type, Is.EqualTo(DataType.Tuple));
                Assert.That(first.Tuple[0].Boolean, Is.True);
                Assert.That(first.Tuple[1].Type, Is.EqualTo(DataType.YieldRequest));
                Assert.That(first.Tuple[1].YieldRequest.Forced, Is.True);
            });

            Assert.That(
                () => script.Call(resumeFunc, coroutineValue, DynValue.NewNumber(42)),
                Throws.TypeOf<ArgumentException>().With.Message.Contains("args must be empty")
            );
        }

        [Test]
        public void IsYieldableReturnsFalseInsideClrCallback()
        {
            Script script = new(CoreModules.PresetComplete);
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

            Assert.Multiple(() =>
            {
                Assert.That(result.Type, Is.EqualTo(DataType.Boolean));
                Assert.That(result.Boolean, Is.False);
            });
        }

        [Test]
        public void WrapWithPcallCapturesErrors()
        {
            Script script = new(CoreModules.PresetComplete);
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

            Assert.Multiple(() =>
            {
                Assert.That(result.Type, Is.EqualTo(DataType.Tuple));
                Assert.That(result.Tuple.Length, Is.GreaterThanOrEqualTo(2));
                Assert.That(result.Tuple[0].Boolean, Is.False);
                Assert.That(result.Tuple[1].String, Does.Contain("wrapped failure"));
            });
        }

        [Test]
        public void WrapWithPcallReturnsYieldedValues()
        {
            Script script = new(CoreModules.PresetComplete);
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
            Assert.Multiple(() =>
            {
                Assert.That(first.Tuple[0].Boolean, Is.True);
                Assert.That(first.Tuple[1].String, Is.EqualTo("first"));
            });

            DynValue second = script.Call(pcall, wrapper);
            Assert.Multiple(() =>
            {
                Assert.That(second.Tuple[0].Boolean, Is.True);
                Assert.That(second.Tuple[1].String, Is.EqualTo("complete"));
                Assert.That(second.Tuple[2].Number, Is.EqualTo(1));
            });
        }

        [Test]
        public void IsYieldableInsidePcallWithinCoroutine()
        {
            Script script = new(CoreModules.PresetComplete);
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

            Assert.Multiple(() =>
            {
                Assert.That(result.Type, Is.EqualTo(DataType.Tuple));
                Assert.That(result.Tuple[0].Boolean, Is.True);
                Assert.That(result.Tuple[1].Type, Is.EqualTo(DataType.Boolean));
                Assert.That(result.Tuple[1].Boolean, Is.True);
            });
        }

        [Test]
        public void WrapWithPcallHandlesTailCalls()
        {
            Script script = new(CoreModules.PresetComplete);
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

            Assert.Multiple(() =>
            {
                Assert.That(result.Type, Is.EqualTo(DataType.Tuple));
                Assert.That(result.Tuple[0].Boolean, Is.True);
                Assert.That(result.Tuple[1].String, Is.EqualTo("tail"));
                Assert.That(result.Tuple[2].String, Is.EqualTo("alpha"));
                Assert.That(result.Tuple[3].Number, Is.EqualTo(42));
            });
        }

        [Test]
        public void IsYieldableInsideXpcallErrorHandlerWithinCoroutine()
        {
            Script script = new(CoreModules.PresetComplete);
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
            Assert.Multiple(() =>
            {
                Assert.That(result.Type, Is.EqualTo(DataType.Tuple));
                Assert.That(result.Tuple[0].Boolean, Is.False);
                Assert.That(result.Tuple[1].String, Does.Contain("boom"));
            });

            DynValue status = script.Call(statusFunc, coroutineValue);
            Assert.That(status.String, Is.EqualTo("dead"));

            DynValue handlerYieldable = script.Globals.Get("handlerYieldable");
            Assert.That(handlerYieldable.Type, Is.EqualTo(DataType.Boolean));
            Assert.That(handlerYieldable.Boolean, Is.False);
        }

        [Test]
        public void CoroutineStatusRemainsAccurateAfterNestedResumes()
        {
            Script script = new(CoreModules.PresetComplete);
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
            Assert.That(firstYield.Type, Is.EqualTo(DataType.String));
            Assert.That(firstYield.String, Is.EqualTo("outer-yield"));

            DynValue outerStatusAfterYield = script.Call(statusFunc, coroutineValue);
            Assert.That(outerStatusAfterYield.String, Is.EqualTo("suspended"));

            DynValue secondResume = coroutineValue.Coroutine.Resume();
            Assert.That(secondResume.Type, Is.EqualTo(DataType.String));
            Assert.That(secondResume.String, Is.EqualTo("inner-done"));

            DynValue outerStatusAfterCompletion = script.Call(statusFunc, coroutineValue);
            Assert.That(outerStatusAfterCompletion.String, Is.EqualTo("dead"));

            DynValue loggedStatuses = script.Globals.Get("loggedStatuses");
            Assert.That(loggedStatuses.Type, Is.EqualTo(DataType.Table));
            Assert.Multiple(() =>
            {
                Assert.That(loggedStatuses.Table.Length, Is.EqualTo(3));
                Assert.That(loggedStatuses.Table.Get(1).String, Is.EqualTo("suspended"));
                Assert.That(loggedStatuses.Table.Get(2).String, Is.EqualTo("suspended"));
                Assert.That(loggedStatuses.Table.Get(3).String, Is.EqualTo("dead"));
            });
        }

        [Test]
        public void ResumeFromDifferentThreadThrowsInvalidOperation()
        {
            Script script = new(CoreModules.PresetComplete);
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
                Assert.That(entered.Wait(TimeSpan.FromSeconds(2)), Is.True);

                Assert.That(
                    () => script.Call(resumeFunc, coroutineValue),
                    Throws
                        .TypeOf<InvalidOperationException>()
                        .With.Message.Contains("Cannot enter the same NovaSharp processor")
                );
            }
            finally
            {
                allowCompletion.Set();
            }

            DynValue result = background.GetAwaiter().GetResult();

            Assert.Multiple(() =>
            {
                Assert.That(result.Type, Is.EqualTo(DataType.Tuple));
                Assert.That(result.Tuple[0].Boolean, Is.True);
                Assert.That(result.Tuple[1].String, Is.EqualTo("done"));
            });
        }

        [Test]
        public void ResumeDeadCoroutineReturnsErrorTuple()
        {
            Script script = new(CoreModules.PresetComplete);
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
            Assert.That(first.Tuple[0].Boolean, Is.True);

            DynValue result = script.Call(resumeFunc, coroutineValue);

            Assert.Multiple(() =>
            {
                Assert.That(result.Type, Is.EqualTo(DataType.Tuple));
                Assert.That(result.Tuple[0].Boolean, Is.False);
                Assert.That(result.Tuple[1].String, Does.Contain("cannot resume dead coroutine"));
            });
        }

        [Test]
        public void YieldReturnsYieldRequestWithArguments()
        {
            Script script = new(CoreModules.PresetComplete);
            DynValue yieldFunc = script.Globals.Get("coroutine").Table.Get("yield");

            DynValue result = script.Call(
                yieldFunc,
                DynValue.NewNumber(7),
                DynValue.NewString("value")
            );

            Assert.Multiple(() =>
            {
                Assert.That(result.Type, Is.EqualTo(DataType.YieldRequest));
                Assert.That(result.YieldRequest.Forced, Is.False);
                ReadOnlySpan<DynValue> values = result.YieldRequest.ReturnValues.Span;
                Assert.That(values.Length, Is.EqualTo(2));
                Assert.That(values[0].Number, Is.EqualTo(7));
                Assert.That(values[1].String, Is.EqualTo("value"));
            });
        }
    }
}
