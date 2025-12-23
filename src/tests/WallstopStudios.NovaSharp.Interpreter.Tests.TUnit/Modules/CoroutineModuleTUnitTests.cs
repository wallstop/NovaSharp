namespace WallstopStudios.NovaSharp.Interpreter.Tests.TUnit.Modules
{
    using System;
    using System.Threading;
    using System.Threading.Tasks;
    using global::TUnit.Assertions;
    using NovaSharp;
    using WallstopStudios.NovaSharp.Interpreter;
    using WallstopStudios.NovaSharp.Interpreter.Compatibility;
    using WallstopStudios.NovaSharp.Interpreter.DataTypes;
    using WallstopStudios.NovaSharp.Interpreter.Errors;
    using WallstopStudios.NovaSharp.Interpreter.Modules;
    using WallstopStudios.NovaSharp.Tests.TestInfrastructure.Scopes;
    using WallstopStudios.NovaSharp.Tests.TestInfrastructure.TUnit;

    public sealed class CoroutineModuleTUnitTests
    {
        // =====================================================
        // coroutine.running() Tests (Version-specific behavior)
        // Lua 5.1: returns only the coroutine
        // Lua 5.2+: returns (coroutine, isMain)
        // =====================================================

        /// <summary>
        /// Verifies that Lua 5.2+ mode returns (coroutine, isMain) from coroutine.running() in main thread.
        /// </summary>
        [global::TUnit.Core.Test]
        [LuaVersionsFrom(LuaCompatibilityVersion.Lua52)]
        public async Task RunningFromMainReturnsMainCoroutine(LuaCompatibilityVersion version)
        {
            Script script = new Script(version, CoreModulePresets.Complete);
            DynValue runningFunc = script.Globals.Get("coroutine").Table.Get("running");

            DynValue result = script.Call(runningFunc);

            await Assert.That(result.Type).IsEqualTo(DataType.Tuple).ConfigureAwait(false);
            await Assert.That(result.Tuple.Length).IsGreaterThanOrEqualTo(2).ConfigureAwait(false);

            DynValue coroutineValue = result.Tuple[0];
            DynValue isMain = result.Tuple[1];

            await Assert.That(coroutineValue.Type).IsEqualTo(DataType.Thread).ConfigureAwait(false);
            await Assert
                .That(coroutineValue.Coroutine.State)
                .IsEqualTo(CoroutineState.Main)
                .ConfigureAwait(false);
            await Assert.That(isMain.Type).IsEqualTo(DataType.Boolean).ConfigureAwait(false);
            await Assert.That(isMain.Boolean).IsTrue().ConfigureAwait(false);
        }

        /// <summary>
        /// Verifies that Lua 5.2+ mode returns isMain=false inside a coroutine.
        /// </summary>
        [global::TUnit.Core.Test]
        [LuaVersionsFrom(LuaCompatibilityVersion.Lua52)]
        public async Task RunningInsideCoroutineReturnsFalse(LuaCompatibilityVersion version)
        {
            Script script = new Script(version, CoreModulePresets.Complete);
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

            await Assert.That(result.Type).IsEqualTo(DataType.Boolean).ConfigureAwait(false);
            await Assert.That(result.Boolean).IsFalse().ConfigureAwait(false);
        }

        // =====================================================
        // coroutine.status() Tests (All versions)
        // =====================================================

        [global::TUnit.Core.Test]
        [AllLuaVersions]
        public async Task StatusReflectsLifecycleAndForceSuspendedStates(
            LuaCompatibilityVersion version
        )
        {
            Script script = new Script(version, CoreModulePresets.Complete);
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
            await Assert.That(initialStatus.String).IsEqualTo("suspended").ConfigureAwait(false);

            coroutineValue.Coroutine.AutoYieldCounter = 1;
            DynValue forced = coroutineValue.Coroutine.Resume();
            await Assert.That(forced.Type).IsEqualTo(DataType.YieldRequest).ConfigureAwait(false);
            await Assert.That(forced.YieldRequest.Forced).IsTrue().ConfigureAwait(false);

            DynValue suspendedStatus = script.Call(statusFunc, coroutineValue);
            await Assert.That(suspendedStatus.String).IsEqualTo("suspended").ConfigureAwait(false);

            coroutineValue.Coroutine.AutoYieldCounter = 0;
            DynValue final = coroutineValue.Coroutine.Resume();
            await Assert.That(final.Type).IsEqualTo(DataType.Number).ConfigureAwait(false);

            DynValue deadStatus = script.Call(statusFunc, coroutineValue);
            await Assert.That(deadStatus.String).IsEqualTo("dead").ConfigureAwait(false);
        }

        [global::TUnit.Core.Test]
        [AllLuaVersions]
        public async Task StatusReturnsRunningForActiveCoroutine(LuaCompatibilityVersion version)
        {
            Script script = new Script(version, CoreModulePresets.Complete);
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

            await Assert.That(result.Type).IsEqualTo(DataType.String).ConfigureAwait(false);
            await Assert.That(result.String).IsEqualTo("running").ConfigureAwait(false);
        }

        /// <summary>
        /// Tests that coroutine.status() on the main coroutine returns 'normal' when called from a child.
        /// Note: This test requires Lua 5.2+ because coroutine.running() returns nil from main in Lua 5.1.
        /// </summary>
        [global::TUnit.Core.Test]
        [LuaVersionsFrom(LuaCompatibilityVersion.Lua52)]
        public async Task StatusReturnsNormalWhenInspectingMainFromChild(
            LuaCompatibilityVersion version
        )
        {
            Script script = new Script(version, CoreModulePresets.Complete);
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

            await Assert.That(result.Type).IsEqualTo(DataType.String).ConfigureAwait(false);
            await Assert.That(result.String).IsEqualTo("normal").ConfigureAwait(false);
        }

        [global::TUnit.Core.Test]
        [AllLuaVersions]
        public async Task StatusThrowsForUnknownStates(LuaCompatibilityVersion version)
        {
            Script script = new Script(version, CoreModulePresets.Complete);
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

            await Assert
                .That(exception.Message)
                .Contains("Unexpected coroutine state")
                .ConfigureAwait(false);
        }

        // =====================================================
        // coroutine.wrap() and coroutine.create() Tests (All versions)
        // =====================================================

        [global::TUnit.Core.Test]
        [AllLuaVersions]
        public async Task WrapRequiresFunctionArgument(LuaCompatibilityVersion version)
        {
            Script script = new Script(version, CoreModulePresets.Complete);
            DynValue wrapFunc = script.Globals.Get("coroutine").Table.Get("wrap");

            ScriptRuntimeException exception = Assert.Throws<ScriptRuntimeException>(() =>
                script.Call(wrapFunc, DynValue.NewNumber(1))
            );

            await Assert
                .That(exception.Message)
                .Contains("bad argument #1 to 'wrap'")
                .ConfigureAwait(false);
        }

        [global::TUnit.Core.Test]
        [AllLuaVersions]
        public async Task CreateRequiresFunctionArgument(LuaCompatibilityVersion version)
        {
            Script script = new Script(version, CoreModulePresets.Complete);
            DynValue createFunc = script.Globals.Get("coroutine").Table.Get("create");

            ScriptRuntimeException exception = Assert.Throws<ScriptRuntimeException>(() =>
                script.Call(createFunc, DynValue.NewString("oops"))
            );

            await Assert
                .That(exception.Message)
                .Contains("bad argument #1 to 'create'")
                .ConfigureAwait(false);
        }

        [global::TUnit.Core.Test]
        [AllLuaVersions]
        public async Task WrapReturnsFunctionThatResumesCoroutine(LuaCompatibilityVersion version)
        {
            Script script = new Script(version, CoreModulePresets.Complete);
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
            await Assert.That(wrapper.Type).IsEqualTo(DataType.ClrFunction).ConfigureAwait(false);

            DynValue first = script.Call(wrapper);
            DynValue second = script.Call(wrapper);
            DynValue third = script.Call(wrapper);
            DynValue final = script.Call(wrapper);

            await Assert.That(first.Number).IsEqualTo(1d).ConfigureAwait(false);
            await Assert.That(second.Number).IsEqualTo(2d).ConfigureAwait(false);
            await Assert.That(third.Number).IsEqualTo(3d).ConfigureAwait(false);
            await Assert.That(final.String).IsEqualTo("done").ConfigureAwait(false);
        }

        // =====================================================
        // coroutine.resume() Tests (All versions)
        // =====================================================

        [global::TUnit.Core.Test]
        [AllLuaVersions]
        public async Task ResumeFlattensResultsAndReportsSuccess(LuaCompatibilityVersion version)
        {
            Script script = new Script(version, CoreModulePresets.Complete);
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
            await Assert.That(first.Type).IsEqualTo(DataType.Tuple).ConfigureAwait(false);
            await Assert.That(first.Tuple.Length).IsEqualTo(3).ConfigureAwait(false);
            await Assert.That(first.Tuple[0].Boolean).IsTrue().ConfigureAwait(false);
            await Assert.That(first.Tuple[1].String).IsEqualTo("yielded").ConfigureAwait(false);
            await Assert.That(first.Tuple[2].Number).IsEqualTo(42d).ConfigureAwait(false);

            DynValue second = script.Call(resumeFunc, coroutineValue);
            await Assert.That(second.Tuple[0].Boolean).IsTrue().ConfigureAwait(false);
            await Assert.That(second.Tuple[1].Number).IsEqualTo(7d).ConfigureAwait(false);
            await Assert.That(second.Tuple[2].Number).IsEqualTo(8d).ConfigureAwait(false);
        }

        [global::TUnit.Core.Test]
        [AllLuaVersions]
        public async Task ResumeReportsErrorsAsFalseWithMessage(LuaCompatibilityVersion version)
        {
            Script script = new Script(version, CoreModulePresets.Complete);
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
            await Assert.That(result.Type).IsEqualTo(DataType.Tuple).ConfigureAwait(false);
            await Assert.That(result.Tuple[0].Boolean).IsFalse().ConfigureAwait(false);
            await Assert.That(result.Tuple[1].String).Contains("boom").ConfigureAwait(false);
        }

        [global::TUnit.Core.Test]
        [AllLuaVersions]
        public async Task ResumeRequiresThreadArgument(LuaCompatibilityVersion version)
        {
            Script script = new Script(version, CoreModulePresets.Complete);
            DynValue resumeFunc = script.Globals.Get("coroutine").Table.Get("resume");

            ScriptRuntimeException exception = Assert.Throws<ScriptRuntimeException>(() =>
                script.Call(resumeFunc, DynValue.NewString("oops"))
            );

            await Assert
                .That(exception.Message)
                .Contains("bad argument #1 to 'resume'")
                .ConfigureAwait(false);
        }

        /// <summary>
        /// Tests that resume flattens nested tuple results. This test uses coroutine.running()
        /// which returns 2 values in Lua 5.2+ (coroutine, isMain), so the total result is 4 values.
        /// </summary>
        [global::TUnit.Core.Test]
        [LuaVersionsFrom(LuaCompatibilityVersion.Lua52)]
        public async Task ResumeFlattensNestedTupleResults(LuaCompatibilityVersion version)
        {
            Script script = new Script(version, CoreModulePresets.Complete);
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

            await Assert.That(result.Type).IsEqualTo(DataType.Tuple).ConfigureAwait(false);
            await Assert.That(result.Tuple.Length).IsEqualTo(4).ConfigureAwait(false);
            await Assert.That(result.Tuple[0].Boolean).IsTrue().ConfigureAwait(false);
            await Assert.That(result.Tuple[1].String).IsEqualTo("tag").ConfigureAwait(false);
            await Assert
                .That(result.Tuple[2].Type)
                .IsEqualTo(DataType.Thread)
                .ConfigureAwait(false);
            await Assert.That(result.Tuple[3].Boolean).IsFalse().ConfigureAwait(false);
        }

        /// <summary>
        /// Tests that resume flattens nested tuple results in Lua 5.1 mode.
        /// In Lua 5.1, coroutine.running() returns only 1 value, so the total result is 3 values.
        /// </summary>
        [global::TUnit.Core.Test]
        [Arguments(LuaCompatibilityVersion.Lua51)]
        public async Task ResumeFlattensNestedTupleResultsLua51(LuaCompatibilityVersion version)
        {
            Script script = new Script(version, CoreModulePresets.Complete);
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

            await Assert.That(result.Type).IsEqualTo(DataType.Tuple).ConfigureAwait(false);
            // Lua 5.1: coroutine.running() returns only 1 value (the coroutine)
            await Assert.That(result.Tuple.Length).IsEqualTo(3).ConfigureAwait(false);
            await Assert.That(result.Tuple[0].Boolean).IsTrue().ConfigureAwait(false);
            await Assert.That(result.Tuple[1].String).IsEqualTo("tag").ConfigureAwait(false);
            await Assert
                .That(result.Tuple[2].Type)
                .IsEqualTo(DataType.Thread)
                .ConfigureAwait(false);
        }

        [global::TUnit.Core.Test]
        [AllLuaVersions]
        public async Task ResumeDeeplyNestedTuplesAreFlattened(LuaCompatibilityVersion version)
        {
            Script script = new Script(version, CoreModulePresets.Complete);
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

            await Assert.That(result.Type).IsEqualTo(DataType.Tuple).ConfigureAwait(false);
            await Assert.That(result.Tuple.Length).IsEqualTo(6).ConfigureAwait(false);
            await Assert.That(result.Tuple[0].Boolean).IsTrue().ConfigureAwait(false);
            await Assert.That(result.Tuple[1].String).IsEqualTo("top").ConfigureAwait(false);
            await Assert.That(result.Tuple[2].Boolean).IsTrue().ConfigureAwait(false);
            await Assert.That(result.Tuple[3].Boolean).IsTrue().ConfigureAwait(false);
            await Assert.That(result.Tuple[4].String).IsEqualTo("deep").ConfigureAwait(false);
            await Assert.That(result.Tuple[5].String).IsEqualTo("value").ConfigureAwait(false);
        }

        [global::TUnit.Core.Test]
        [AllLuaVersions]
        public async Task ResumeForwardsArgumentsToCoroutine(LuaCompatibilityVersion version)
        {
            Script script = new Script(version, CoreModulePresets.Complete);
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

            await Assert.That(result.Type).IsEqualTo(DataType.Tuple).ConfigureAwait(false);
            await Assert.That(result.Tuple.Length).IsEqualTo(2).ConfigureAwait(false);
            await Assert.That(result.Tuple[0].Boolean).IsTrue().ConfigureAwait(false);
            await Assert.That(result.Tuple[1].Number).IsEqualTo(10d).ConfigureAwait(false);
        }

        [global::TUnit.Core.Test]
        [AllLuaVersions]
        public async Task ResumeFlattensTrailingTupleResults(LuaCompatibilityVersion version)
        {
            Script script = new Script(version, CoreModulePresets.Complete);
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

            await Assert.That(result.Type).IsEqualTo(DataType.Tuple).ConfigureAwait(false);
            await Assert.That(result.Tuple.Length).IsEqualTo(4).ConfigureAwait(false);
            await Assert.That(result.Tuple[0].Boolean).IsTrue().ConfigureAwait(false);
            await Assert.That(result.Tuple[1].String).IsEqualTo("outer").ConfigureAwait(false);
            await Assert
                .That(result.Tuple[2].String)
                .IsEqualTo("inner-value")
                .ConfigureAwait(false);
            await Assert.That(result.Tuple[3].Number).IsEqualTo(99d).ConfigureAwait(false);
        }

        [global::TUnit.Core.Test]
        [AllLuaVersions]
        public async Task WrapForwardsArgumentsToCoroutineFunction(LuaCompatibilityVersion version)
        {
            Script script = new Script(version, CoreModulePresets.Complete);
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

            await Assert.That(result.String).IsEqualTo("alpha-beta-gamma").ConfigureAwait(false);
        }

        // =====================================================
        // coroutine.isyieldable() Tests (Lua 5.3+ only)
        // This function was added in Lua 5.3
        // =====================================================

        /// <summary>
        /// Tests that coroutine.isyieldable() returns false when called from the main coroutine.
        /// This function was added in Lua 5.3.
        /// </summary>
        [global::TUnit.Core.Test]
        [LuaVersionsFrom(LuaCompatibilityVersion.Lua53)]
        public async Task IsYieldableReturnsFalseOnMainCoroutine(LuaCompatibilityVersion version)
        {
            Script script = new Script(version, CoreModulePresets.Complete);
            DynValue isYieldableFunc = script.Globals.Get("coroutine").Table.Get("isyieldable");

            DynValue result = script.Call(isYieldableFunc);

            await Assert.That(result.Type).IsEqualTo(DataType.Boolean).ConfigureAwait(false);
            await Assert.That(result.Boolean).IsFalse().ConfigureAwait(false);
        }

        /// <summary>
        /// Tests that coroutine.isyieldable() is nil in Lua 5.1 and 5.2 (function doesn't exist).
        /// </summary>
        [global::TUnit.Core.Test]
        [LuaVersionsUntil(LuaCompatibilityVersion.Lua52)]
        public async Task IsYieldableShouldBeNilInPreLua53(LuaCompatibilityVersion version)
        {
            Script script = new Script(version, CoreModulePresets.Complete);
            DynValue isYieldableFunc = script.Globals.Get("coroutine").Table.Get("isyieldable");

            await Assert.That(isYieldableFunc.IsNil()).IsTrue().ConfigureAwait(false);
        }

        /// <summary>
        /// Tests that coroutine.isyieldable() returns true when called inside a coroutine.
        /// This function was added in Lua 5.3.
        /// </summary>
        [global::TUnit.Core.Test]
        [LuaVersionsFrom(LuaCompatibilityVersion.Lua53)]
        public async Task IsYieldableReturnsTrueInsideCoroutine(LuaCompatibilityVersion version)
        {
            Script script = new Script(version, CoreModulePresets.Complete);
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

            await Assert.That(result.Type).IsEqualTo(DataType.Boolean).ConfigureAwait(false);
            await Assert.That(result.Boolean).IsTrue().ConfigureAwait(false);
        }

        // =====================================================
        // Error Propagation Tests (All versions)
        // =====================================================

        [global::TUnit.Core.Test]
        [AllLuaVersions]
        public async Task WrapPropagatesErrorsToCaller(LuaCompatibilityVersion version)
        {
            Script script = new Script(version, CoreModulePresets.Complete);
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

            await Assert.That(exception.Message).Contains("wrap boom").ConfigureAwait(false);
        }

        [global::TUnit.Core.Test]
        [AllLuaVersions]
        public async Task WrapPropagatesErrorsAfterYield(LuaCompatibilityVersion version)
        {
            Script script = new Script(version, CoreModulePresets.Complete);
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

            await Assert.That(first.Type).IsEqualTo(DataType.String).ConfigureAwait(false);
            await Assert.That(first.String).IsEqualTo("first").ConfigureAwait(false);

            ScriptRuntimeException exception = Assert.Throws<ScriptRuntimeException>(() =>
                script.Call(wrapper)
            );

            await Assert.That(exception.Message).Contains("wrap later").ConfigureAwait(false);
        }

        // =====================================================
        // Force Suspend and Auto-Yield Tests (All versions)
        // =====================================================

        [global::TUnit.Core.Test]
        [global::TUnit.Core.Arguments(LuaCompatibilityVersion.Lua51)]
        [global::TUnit.Core.Arguments(LuaCompatibilityVersion.Lua52)]
        [global::TUnit.Core.Arguments(LuaCompatibilityVersion.Lua53)]
        [global::TUnit.Core.Arguments(LuaCompatibilityVersion.Lua54)]
        [global::TUnit.Core.Arguments(LuaCompatibilityVersion.Lua55)]
        public async Task ResumeForceSuspendedCoroutineRejectsArguments(
            LuaCompatibilityVersion version
        )
        {
            Script script = new Script(version, CoreModulePresets.Complete);
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
            await Assert.That(first.Type).IsEqualTo(DataType.Tuple).ConfigureAwait(false);
            await Assert.That(first.Tuple[0].Boolean).IsTrue().ConfigureAwait(false);
            await Assert
                .That(first.Tuple[1].Type)
                .IsEqualTo(DataType.YieldRequest)
                .ConfigureAwait(false);
            await Assert.That(first.Tuple[1].YieldRequest.Forced).IsTrue().ConfigureAwait(false);

            ArgumentException exception = Assert.Throws<ArgumentException>(() =>
                script.Call(resumeFunc, coroutineValue, DynValue.NewNumber(42))
            );

            await Assert
                .That(exception.Message)
                .Contains("args must be empty")
                .ConfigureAwait(false);
        }

        /// <summary>
        /// Tests that coroutine.isyieldable() returns false inside a CLR callback.
        /// This function was added in Lua 5.3.
        /// </summary>
        [global::TUnit.Core.Test]
        [global::TUnit.Core.Arguments(LuaCompatibilityVersion.Lua53)]
        [global::TUnit.Core.Arguments(LuaCompatibilityVersion.Lua54)]
        [global::TUnit.Core.Arguments(LuaCompatibilityVersion.Lua55)]
        public async Task IsYieldableReturnsFalseInsideClrCallback(LuaCompatibilityVersion version)
        {
            Script script = new Script(version, CoreModulePresets.Complete);
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

            await Assert.That(result.Type).IsEqualTo(DataType.Boolean).ConfigureAwait(false);
            await Assert.That(result.Boolean).IsFalse().ConfigureAwait(false);
        }

        // =====================================================
        // pcall/xpcall with Coroutines Tests (All versions, except isyieldable tests)
        // =====================================================

        [global::TUnit.Core.Test]
        [AllLuaVersions]
        public async Task WrapWithPcallCapturesErrors(LuaCompatibilityVersion version)
        {
            Script script = new Script(version, CoreModulePresets.Complete);
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

            await Assert.That(result.Type).IsEqualTo(DataType.Tuple).ConfigureAwait(false);
            await Assert.That(result.Tuple.Length).IsGreaterThanOrEqualTo(2).ConfigureAwait(false);
            await Assert.That(result.Tuple[0].Boolean).IsFalse().ConfigureAwait(false);
            await Assert
                .That(result.Tuple[1].String)
                .Contains("wrapped failure")
                .ConfigureAwait(false);
        }

        [global::TUnit.Core.Test]
        [AllLuaVersions]
        public async Task WrapWithPcallReturnsYieldedValues(LuaCompatibilityVersion version)
        {
            Script script = new Script(version, CoreModulePresets.Complete);
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
            await Assert.That(first.Tuple[0].Boolean).IsTrue().ConfigureAwait(false);
            await Assert.That(first.Tuple[1].String).IsEqualTo("first").ConfigureAwait(false);

            DynValue second = script.Call(pcall, wrapper);
            await Assert.That(second.Tuple[0].Boolean).IsTrue().ConfigureAwait(false);
            await Assert.That(second.Tuple[1].String).IsEqualTo("complete").ConfigureAwait(false);
            await Assert.That(second.Tuple[2].Number).IsEqualTo(1d).ConfigureAwait(false);
        }

        /// <summary>
        /// Tests that coroutine.isyieldable() returns true inside pcall within a coroutine.
        /// This function was added in Lua 5.3.
        /// </summary>
        [global::TUnit.Core.Test]
        [LuaVersionsFrom(LuaCompatibilityVersion.Lua53)]
        public async Task IsYieldableInsidePcallWithinCoroutine(LuaCompatibilityVersion version)
        {
            Script script = new Script(version, CoreModulePresets.Complete);
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

            await Assert.That(result.Type).IsEqualTo(DataType.Tuple).ConfigureAwait(false);
            await Assert.That(result.Tuple[0].Boolean).IsTrue().ConfigureAwait(false);
            await Assert
                .That(result.Tuple[1].Type)
                .IsEqualTo(DataType.Boolean)
                .ConfigureAwait(false);
            await Assert.That(result.Tuple[1].Boolean).IsTrue().ConfigureAwait(false);
        }

        [global::TUnit.Core.Test]
        [AllLuaVersions]
        public async Task WrapWithPcallHandlesTailCalls(LuaCompatibilityVersion version)
        {
            Script script = new Script(version, CoreModulePresets.Complete);
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

            await Assert.That(result.Type).IsEqualTo(DataType.Tuple).ConfigureAwait(false);
            await Assert.That(result.Tuple[0].Boolean).IsTrue().ConfigureAwait(false);
            await Assert.That(result.Tuple[1].String).IsEqualTo("tail").ConfigureAwait(false);
            await Assert.That(result.Tuple[2].String).IsEqualTo("alpha").ConfigureAwait(false);
            await Assert.That(result.Tuple[3].Number).IsEqualTo(42d).ConfigureAwait(false);
        }

        /// <summary>
        /// Tests that coroutine.isyieldable() returns false inside xpcall error handler.
        /// This function was added in Lua 5.3.
        /// </summary>
        [global::TUnit.Core.Test]
        [LuaVersionsFrom(LuaCompatibilityVersion.Lua53)]
        public async Task IsYieldableInsideXpcallErrorHandlerWithinCoroutine(
            LuaCompatibilityVersion version
        )
        {
            Script script = new Script(version, CoreModulePresets.Complete);
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
            await Assert.That(result.Type).IsEqualTo(DataType.Tuple).ConfigureAwait(false);
            await Assert.That(result.Tuple[0].Boolean).IsFalse().ConfigureAwait(false);
            await Assert.That(result.Tuple[1].String).Contains("boom").ConfigureAwait(false);

            DynValue status = script.Call(statusFunc, coroutineValue);
            await Assert.That(status.String).IsEqualTo("dead").ConfigureAwait(false);

            DynValue handlerYieldable = script.Globals.Get("handlerYieldable");
            await Assert
                .That(handlerYieldable.Type)
                .IsEqualTo(DataType.Boolean)
                .ConfigureAwait(false);
            await Assert.That(handlerYieldable.Boolean).IsFalse().ConfigureAwait(false);
        }

        // =====================================================
        // Nested Coroutines and Status Tests (All versions)
        // =====================================================

        [global::TUnit.Core.Test]
        [AllLuaVersions]
        public async Task CoroutineStatusRemainsAccurateAfterNestedResumes(
            LuaCompatibilityVersion version
        )
        {
            Script script = new Script(version, CoreModulePresets.Complete);
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
            await Assert.That(firstYield.Type).IsEqualTo(DataType.String).ConfigureAwait(false);
            await Assert.That(firstYield.String).IsEqualTo("outer-yield").ConfigureAwait(false);

            DynValue outerStatusAfterYield = script.Call(statusFunc, coroutineValue);
            await Assert
                .That(outerStatusAfterYield.String)
                .IsEqualTo("suspended")
                .ConfigureAwait(false);

            DynValue secondResume = coroutineValue.Coroutine.Resume();
            await Assert.That(secondResume.Type).IsEqualTo(DataType.String).ConfigureAwait(false);
            await Assert.That(secondResume.String).IsEqualTo("inner-done").ConfigureAwait(false);

            DynValue outerStatusAfterCompletion = script.Call(statusFunc, coroutineValue);
            await Assert
                .That(outerStatusAfterCompletion.String)
                .IsEqualTo("dead")
                .ConfigureAwait(false);

            DynValue loggedStatuses = script.Globals.Get("loggedStatuses");
            await Assert.That(loggedStatuses.Type).IsEqualTo(DataType.Table).ConfigureAwait(false);
            await Assert.That(loggedStatuses.Table.Length).IsEqualTo(3).ConfigureAwait(false);
            await Assert
                .That(loggedStatuses.Table.Get(1).String)
                .IsEqualTo("suspended")
                .ConfigureAwait(false);
            await Assert
                .That(loggedStatuses.Table.Get(2).String)
                .IsEqualTo("suspended")
                .ConfigureAwait(false);
            await Assert
                .That(loggedStatuses.Table.Get(3).String)
                .IsEqualTo("dead")
                .ConfigureAwait(false);
        }

        // =====================================================
        // Threading and Concurrency Tests (All versions)
        // =====================================================

        /// <summary>
        /// Tests that attempting to resume a coroutine from a different thread while it's
        /// already running throws InvalidOperationException. This validates thread-safety
        /// guarantees of the interpreter.
        /// </summary>
        [global::TUnit.Core.Test]
        [AllLuaVersions]
        public async Task ResumeFromDifferentThreadThrowsInvalidOperation(
            LuaCompatibilityVersion version
        )
        {
            // Use TaskCompletionSource for async signaling without blocking threads
            TaskCompletionSource<bool> callbackEntered = new(
                TaskCreationOptions.RunContinuationsAsynchronously
            );
            TaskCompletionSource<bool> allowCompletion = new(
                TaskCreationOptions.RunContinuationsAsynchronously
            );

            Script script = new Script(version, CoreModulePresets.Complete);

            script.Globals["waitForSignal"] = DynValue.NewCallback(
                (_, _) =>
                {
                    callbackEntered.TrySetResult(true);
                    // Block until we're allowed to complete
                    allowCompletion.Task.GetAwaiter().GetResult();
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

            // Start background task - use ConfigureAwait(false) to ensure continuation runs on thread pool
            Task<DynValue> background = Task.Run(() => script.Call(resumeFunc, coroutineValue));

            // Wait for the callback to be invoked with timeout
            using CancellationTokenSource cts = new(TimeSpan.FromSeconds(10));
            try
            {
                await callbackEntered.Task.WaitAsync(cts.Token).ConfigureAwait(false);
            }
            catch (OperationCanceledException)
            {
                // Cleanup and fail with diagnostic info
                allowCompletion.TrySetResult(true);
                throw new TimeoutException(
                    $"Timeout waiting for callback. Background task status: {background.Status}"
                );
            }

            // Now try to resume from this thread - should throw
            InvalidOperationException exception = Assert.Throws<InvalidOperationException>(() =>
                script.Call(resumeFunc, coroutineValue)
            );

            await Assert
                .That(exception.Message)
                .Contains("Cannot enter the same NovaSharp processor")
                .ConfigureAwait(false);

            // Allow the background task to complete
            allowCompletion.TrySetResult(true);

            DynValue result = await background.ConfigureAwait(false);
            await Assert.That(result.Type).IsEqualTo(DataType.Tuple).ConfigureAwait(false);
            await Assert.That(result.Tuple[0].Boolean).IsTrue().ConfigureAwait(false);
            await Assert.That(result.Tuple[1].String).IsEqualTo("done").ConfigureAwait(false);
        }

        /// <summary>
        /// Tests that multiple concurrent threads trying to resume the same coroutine
        /// results in only one succeeding and the others throwing InvalidOperationException.
        /// </summary>
        [global::TUnit.Core.Test]
        [AllLuaVersions]
        public async Task MultipleConcurrentResumeAttemptsOnlyOneSucceeds(
            LuaCompatibilityVersion version
        )
        {
            const int ConcurrentThreads = 4;

            TaskCompletionSource<bool> callbackEntered = new(
                TaskCreationOptions.RunContinuationsAsynchronously
            );
            TaskCompletionSource<bool> allowCompletion = new(
                TaskCreationOptions.RunContinuationsAsynchronously
            );
            TaskCompletionSource<bool> startSignal = new(
                TaskCreationOptions.RunContinuationsAsynchronously
            );

            Script script = new Script(version, CoreModulePresets.Complete);

            script.Globals["waitForSignal"] = DynValue.NewCallback(
                (_, _) =>
                {
                    callbackEntered.TrySetResult(true);
                    allowCompletion.Task.GetAwaiter().GetResult();
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

            int successCount = 0;
            int failureCount = 0;
            InvalidOperationException caughtException = null;
            int readyCount = 0;

            Task[] tasks = new Task[ConcurrentThreads];
            for (int i = 0; i < ConcurrentThreads; i++)
            {
                tasks[i] = Task.Run(async () =>
                {
                    Interlocked.Increment(ref readyCount);
                    await startSignal.Task.ConfigureAwait(false);

                    try
                    {
                        script.Call(resumeFunc, coroutineValue);
                        Interlocked.Increment(ref successCount);
                    }
                    catch (InvalidOperationException ex)
                    {
                        Interlocked.Increment(ref failureCount);
                        Interlocked.CompareExchange(ref caughtException, ex, null);
                    }
                });
            }

            // Wait for all threads to be ready (simple spin-wait for task startup)
            using CancellationTokenSource cts = new(TimeSpan.FromSeconds(10));
            while (Volatile.Read(ref readyCount) < ConcurrentThreads)
            {
                cts.Token.ThrowIfCancellationRequested();
                await Task.Yield();
            }

            // Release all threads simultaneously
            startSignal.TrySetResult(true);

            // Wait for the callback to be invoked with timeout
            await callbackEntered.Task.WaitAsync(cts.Token).ConfigureAwait(false);

            // Allow completion
            allowCompletion.TrySetResult(true);

            // Wait for all tasks to complete
            await Task.WhenAll(tasks).ConfigureAwait(false);

            // Exactly one thread should have succeeded
            await Assert
                .That(successCount)
                .IsEqualTo(1)
                .Because(
                    $"Expected exactly one thread to succeed, but {successCount} succeeded and {failureCount} failed"
                )
                .ConfigureAwait(false);

            // All other threads should have failed with InvalidOperationException
            await Assert.That(failureCount).IsEqualTo(ConcurrentThreads - 1).ConfigureAwait(false);

            // Verify the exception message
            await Assert.That(caughtException).IsNotNull().ConfigureAwait(false);
            await Assert
                .That(caughtException.Message)
                .Contains("Cannot enter the same NovaSharp processor")
                .ConfigureAwait(false);
        }

        /// <summary>
        /// Tests that separate Script instances can execute concurrently on different threads
        /// without interference. Each script has its own processor and state.
        /// </summary>
        [global::TUnit.Core.Test]
        [AllLuaVersions]
        public async Task SeparateScriptInstancesCanRunConcurrently(LuaCompatibilityVersion version)
        {
            const int ConcurrentScripts = 4;

            TaskCompletionSource<bool> proceedSignal = new(
                TaskCreationOptions.RunContinuationsAsynchronously
            );
            int readyCount = 0;
            int completionCount = 0;
            ScriptRuntimeException caughtException = null;

            Task<int>[] tasks = new Task<int>[ConcurrentScripts];
            for (int i = 0; i < ConcurrentScripts; i++)
            {
                int scriptIndex = i;
                tasks[i] = Task.Run(async () =>
                {
                    try
                    {
                        Script script = new Script(version, CoreModulePresets.Complete);
                        script.Globals["scriptIndex"] = DynValue.NewNumber(scriptIndex);
                        script.Globals["waitForProceed"] = DynValue.NewCallback(
                            (_, _) =>
                            {
                                Interlocked.Increment(ref readyCount);
                                proceedSignal.Task.GetAwaiter().GetResult();
                                return DynValue.Nil;
                            }
                        );

                        DynValue result = script.DoString(
                            @"
                            waitForProceed()
                            local sum = 0
                            for i = 1, 100 do
                                sum = sum + i
                            end
                            return scriptIndex * 1000 + sum
                        "
                        );

                        Interlocked.Increment(ref completionCount);
                        return (int)result.Number;
                    }
                    catch (ScriptRuntimeException ex)
                    {
                        Interlocked.CompareExchange(ref caughtException, ex, null);
                        return -1;
                    }
                });
            }

            // Wait for all scripts to reach the synchronization point
            using CancellationTokenSource cts = new(TimeSpan.FromSeconds(10));
            while (Volatile.Read(ref readyCount) < ConcurrentScripts)
            {
                cts.Token.ThrowIfCancellationRequested();
                await Task.Delay(1, cts.Token).ConfigureAwait(false);
            }

            // Release all scripts to proceed
            proceedSignal.TrySetResult(true);

            // Wait for all tasks to complete
            int[] results = await Task.WhenAll(tasks).ConfigureAwait(false);

            // All scripts should have completed successfully
            await Assert.That(completionCount).IsEqualTo(ConcurrentScripts).ConfigureAwait(false);

            await Assert.That(caughtException).IsNull().ConfigureAwait(false);

            // Verify each script computed the correct result
            // sum(1..100) = 5050, so result = scriptIndex * 1000 + 5050
            for (int i = 0; i < ConcurrentScripts; i++)
            {
                int expectedResult = i * 1000 + 5050;
                await Assert.That(results[i]).IsEqualTo(expectedResult).ConfigureAwait(false);
            }
        }

        [global::TUnit.Core.Test]
        [AllLuaVersions]
        public async Task ResumeDeadCoroutineReturnsErrorTuple(LuaCompatibilityVersion version)
        {
            Script script = new Script(version, CoreModulePresets.Complete);
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
            await Assert.That(first.Tuple[0].Boolean).IsTrue().ConfigureAwait(false);

            DynValue result = script.Call(resumeFunc, coroutineValue);
            await Assert.That(result.Type).IsEqualTo(DataType.Tuple).ConfigureAwait(false);
            await Assert.That(result.Tuple[0].Boolean).IsFalse().ConfigureAwait(false);
            await Assert
                .That(result.Tuple[1].String)
                .Contains("cannot resume dead coroutine")
                .ConfigureAwait(false);
        }

        // =====================================================
        // coroutine.yield() Tests (All versions)
        // =====================================================

        [global::TUnit.Core.Test]
        [AllLuaVersions]
        public async Task YieldReturnsYieldRequestWithArguments(LuaCompatibilityVersion version)
        {
            Script script = new Script(version, CoreModulePresets.Complete);
            DynValue yieldFunc = script.Globals.Get("coroutine").Table.Get("yield");

            DynValue result = script.Call(
                yieldFunc,
                DynValue.NewNumber(7),
                DynValue.NewString("value")
            );

            await Assert.That(result.Type).IsEqualTo(DataType.YieldRequest).ConfigureAwait(false);
            await Assert.That(result.YieldRequest.Forced).IsFalse().ConfigureAwait(false);

            ReadOnlyMemory<DynValue> values = result.YieldRequest.ReturnValues;
            await Assert.That(values.Length).IsEqualTo(2).ConfigureAwait(false);
            await Assert.That(values.Span[0].Number).IsEqualTo(7d).ConfigureAwait(false);
            await Assert.That(values.Span[1].String).IsEqualTo("value").ConfigureAwait(false);
        }

        // =====================================================
        // coroutine.running() Version Parity Tests (Additional)
        // These are duplicate/supplemental tests that already exist above
        // =====================================================
        // Lua 5.1: coroutine.running() returns only the coroutine
        // Lua 5.2+: coroutine.running() returns (coroutine, isMain)

        /// <summary>
        /// Verifies that Lua 5.1 mode returns only the coroutine from coroutine.running().
        /// </summary>
        [global::TUnit.Core.Test]
        [Arguments(LuaCompatibilityVersion.Lua51)]
        public async Task RunningReturnsOnlyCoroutineInLua51(LuaCompatibilityVersion version)
        {
            Script script = new Script(version, CoreModulePresets.Complete);

            DynValue result = script.DoString(
                @"
                -- Count how many values coroutine.running returns
                local function countReturns()
                    return select('#', coroutine.running())
                end
                
                local co = coroutine.create(countReturns)
                local ok, count = coroutine.resume(co)
                return count
                "
            );

            // In Lua 5.1, coroutine.running() returns only 1 value
            await Assert.That(result.Number).IsEqualTo(1).ConfigureAwait(false);
        }

        /// <summary>
        /// Verifies that Lua 5.2+ mode returns (coroutine, isMain) from coroutine.running().
        /// </summary>
        [global::TUnit.Core.Test]
        [LuaVersionsFrom(LuaCompatibilityVersion.Lua52)]
        public async Task RunningReturnsTupleInLua52Plus(LuaCompatibilityVersion version)
        {
            Script script = new Script(version, CoreModulePresets.Complete);

            DynValue result = script.DoString(
                @"
                -- Count how many values coroutine.running returns
                local function countReturns()
                    return select('#', coroutine.running())
                end
                
                local co = coroutine.create(countReturns)
                local ok, count = coroutine.resume(co)
                return count
                "
            );

            // In Lua 5.2+, coroutine.running() returns 2 values
            await Assert.That(result.Number).IsEqualTo(2).ConfigureAwait(false);
        }

        /// <summary>
        /// Verifies that Lua 5.1 mode's coroutine.running() returns the correct coroutine.
        /// </summary>
        [global::TUnit.Core.Test]
        [Arguments(LuaCompatibilityVersion.Lua51)]
        public async Task RunningReturnsCorrectCoroutineInLua51(LuaCompatibilityVersion version)
        {
            Script script = new Script(version, CoreModulePresets.Complete);

            DynValue result = script.DoString(
                @"
                local function checkRunning()
                    local running = coroutine.running()
                    -- Should be a thread value, not nil
                    return type(running)
                end
                
                local co = coroutine.create(checkRunning)
                local ok, result = coroutine.resume(co)
                return result
                "
            );

            await Assert.That(result.String).IsEqualTo("thread").ConfigureAwait(false);
        }

        /// <summary>
        /// Verifies that Lua 5.2+ mode's coroutine.running() returns isMain=false inside a coroutine.
        /// </summary>
        [global::TUnit.Core.Test]
        [LuaVersionsFrom(LuaCompatibilityVersion.Lua52)]
        public async Task RunningReturnsIsMainFalseInsideCoroutineInLua52Plus(
            LuaCompatibilityVersion version
        )
        {
            Script script = new Script(version, CoreModulePresets.Complete);

            DynValue result = script.DoString(
                @"
                local function checkIsMain()
                    local _, isMain = coroutine.running()
                    return isMain
                end
                
                local co = coroutine.create(checkIsMain)
                local ok, isMain = coroutine.resume(co)
                return isMain
                "
            );

            await Assert.That(result.Boolean).IsFalse().ConfigureAwait(false);
        }

        /// <summary>
        /// Verifies that Lua 5.2+ mode's coroutine.running() returns isMain=true in the main thread.
        /// </summary>
        [global::TUnit.Core.Test]
        [LuaVersionsFrom(LuaCompatibilityVersion.Lua52)]
        public async Task RunningReturnsIsMainTrueInMainThreadInLua52Plus(
            LuaCompatibilityVersion version
        )
        {
            Script script = new Script(version, CoreModulePresets.Complete);

            DynValue result = script.DoString(
                @"
                local _, isMain = coroutine.running()
                return isMain
                "
            );

            await Assert.That(result.Boolean).IsTrue().ConfigureAwait(false);
        }

        private static Script CreateScript()
        {
            return new Script(CoreModulePresets.Complete);
        }
    }
}
