namespace WallstopStudios.NovaSharp.Interpreter.Tests.TUnit.Units.DataTypes
{
    using System;
    using System.Threading.Tasks;
    using global::TUnit.Assertions;
    using WallstopStudios.NovaSharp.Interpreter;
    using WallstopStudios.NovaSharp.Interpreter.Compatibility;
    using WallstopStudios.NovaSharp.Interpreter.DataTypes;
    using WallstopStudios.NovaSharp.Interpreter.Errors;
    using WallstopStudios.NovaSharp.Interpreter.Modules;
    using WallstopStudios.NovaSharp.Interpreter.Sandboxing;
    using WallstopStudios.NovaSharp.Tests.TestInfrastructure.TUnit;

    public sealed class TailCallTUnitTests
    {
        [global::TUnit.Core.Test]
        [AllLuaVersions]
        public async Task TailRecursionHandlesThousandsOfFrames(LuaCompatibilityVersion version)
        {
            Script script = new(version);
            DynValue result = script.DoString(
                @"
                local function accumulate(n, acc)
                    if n == 0 then
                        return acc
                    end
                    return accumulate(n - 1, acc + 1)
                end

                return accumulate(20000, 0)
            "
            );

            await Assert.That(result.Type).IsEqualTo(DataType.Number).ConfigureAwait(false);
            await Assert.That(result.Number).IsEqualTo(20000).ConfigureAwait(false);
        }

        [global::TUnit.Core.Test]
        [AllLuaVersions]
        public async Task TailCallPreservesMultipleReturnValues(LuaCompatibilityVersion version)
        {
            Script script = new(version);
            DynValue result = script.DoString(
                @"
                local function id(a, b, c)
                    return a, b, c
                end

                local function forward(...)
                    return id(...)
                end

                return forward(1, 2, 3)
            "
            );

            await Assert.That(result.Type).IsEqualTo(DataType.Tuple).ConfigureAwait(false);
            await Assert.That(result.Tuple.Length).IsEqualTo(3).ConfigureAwait(false);
            await Assert.That(result.Tuple[0].Number).IsEqualTo(1).ConfigureAwait(false);
            await Assert.That(result.Tuple[1].Number).IsEqualTo(2).ConfigureAwait(false);
            await Assert.That(result.Tuple[2].Number).IsEqualTo(3).ConfigureAwait(false);
        }

        [global::TUnit.Core.Test]
        [AllLuaVersions]
        public async Task RecursiveSumMatchesArithmeticBaseline(LuaCompatibilityVersion version)
        {
            Script script = new(version);
            DynValue result = script.DoString(
                @"
                local function recsum(n, partial)
                    if n == 0 then
                        return partial
                    end
                    return recsum(n - 1, partial + n)
                end

                return recsum(10, 0)
            "
            );

            await Assert.That(result.Type).IsEqualTo(DataType.Number).ConfigureAwait(false);
            await Assert.That(result.Number).IsEqualTo(55).ConfigureAwait(false);
        }

        [global::TUnit.Core.Test]
        [AllLuaVersions]
        public async Task RecursiveSumHandlesVeryDeepTailRecursion(LuaCompatibilityVersion version)
        {
            Script script = new(version);
            DynValue result = script.DoString(
                @"
                local function recsum(n, partial)
                    if n == 0 then
                        return partial
                    end
                    return recsum(n - 1, partial + n)
                end

                return recsum(70000, 0)
            "
            );

            await Assert.That(result.Type).IsEqualTo(DataType.Number).ConfigureAwait(false);
            await Assert.That(result.Number).IsEqualTo(2450035000.0).ConfigureAwait(false);
        }

        [global::TUnit.Core.Test]
        [LuaVersionsFrom(LuaCompatibilityVersion.Lua52)]
        public async Task TailRecursionDoesNotGrowDebugStack(LuaCompatibilityVersion version)
        {
            Script script = new(version, CoreModules.Debug);
            DynValue result = script.DoString(
                @"
                local max_depth = 0

                local function count_stack()
                    local level = 1
                    while debug.getinfo(level, 'n') do
                        level = level + 1
                    end

                    if level > max_depth then
                        max_depth = level
                    end
                end

                local function recur(n)
                    count_stack()
                    if n == 0 then
                        return max_depth
                    end

                    return recur(n - 1)
                end

                return recur(100)
            "
            );

            await Assert.That(result.Type).IsEqualTo(DataType.Number).ConfigureAwait(false);
            await Assert.That(result.Number).IsLessThanOrEqualTo(8).ConfigureAwait(false);
        }

        [global::TUnit.Core.Test]
        [LuaVersionsFrom(LuaCompatibilityVersion.Lua52)]
        public async Task DebugGetInfoReportsTailCallFrames(LuaCompatibilityVersion version)
        {
            Script script = new(version, CoreModules.Debug);
            DynValue result = script.DoString(
                @"
                local function target()
                    return debug.getinfo(1, 't').istailcall
                end

                local function caller()
                    return target()
                end

                return caller()
            "
            );

            await Assert.That(result.Type).IsEqualTo(DataType.Boolean).ConfigureAwait(false);
            await Assert.That(result.Boolean).IsTrue().ConfigureAwait(false);
        }

        [global::TUnit.Core.Test]
        [LuaVersionsFrom(LuaCompatibilityVersion.Lua52)]
        public async Task DebugGetInfoDefaultIncludesTailCallFlag(LuaCompatibilityVersion version)
        {
            Script script = new(version, CoreModules.Debug);
            DynValue result = script.DoString(
                @"
                local function target()
                    return debug.getinfo(1).istailcall
                end

                local function caller()
                    return target()
                end

                return caller()
            "
            );

            await Assert.That(result.Type).IsEqualTo(DataType.Boolean).ConfigureAwait(false);
            await Assert.That(result.Boolean).IsTrue().ConfigureAwait(false);
        }

        [global::TUnit.Core.Test]
        [LuaVersionsFrom(LuaCompatibilityVersion.Lua52)]
        public async Task DebugGetInfoOmitsNameForTailCalledFrames(LuaCompatibilityVersion version)
        {
            Script script = new(version, CoreModules.Debug);
            DynValue result = script.DoString(
                @"
                local function target()
                    local info = debug.getinfo(1, 'nSt')
                    return info.name == nil, info.namewhat, info.istailcall
                end

                local function caller()
                    return target()
                end

                return caller()
            "
            );

            await Assert.That(result.Type).IsEqualTo(DataType.Tuple).ConfigureAwait(false);
            await Assert.That(result.Tuple.Length).IsEqualTo(3).ConfigureAwait(false);
            await Assert.That(result.Tuple[0].Boolean).IsTrue().ConfigureAwait(false);
            await Assert.That(result.Tuple[1].String).IsEqualTo(string.Empty).ConfigureAwait(false);
            await Assert.That(result.Tuple[2].Boolean).IsTrue().ConfigureAwait(false);
        }

        [global::TUnit.Core.Test]
        [LuaVersionsFrom(LuaCompatibilityVersion.Lua52)]
        public async Task DebugGetInfoReportsClrFrameAsNotTailCalled(
            LuaCompatibilityVersion version
        )
        {
            Script script = new(version, CoreModules.Debug);
            DynValue result = script.DoString(
                @"
                local function caller()
                    return debug.getinfo(0, 't').istailcall
                end

                return caller()
            "
            );

            await Assert.That(result.Type).IsEqualTo(DataType.Boolean).ConfigureAwait(false);
            await Assert.That(result.Boolean).IsFalse().ConfigureAwait(false);
        }

        [global::TUnit.Core.Test]
        [LuaVersionsFrom(LuaCompatibilityVersion.Lua52)]
        public async Task DebugGetInfoDefaultReportsClrFrameAsNotTailCalled(
            LuaCompatibilityVersion version
        )
        {
            Script script = new(version, CoreModules.Debug);
            DynValue result = script.DoString(
                @"
                local function caller()
                    return debug.getinfo(0).istailcall
                end

                return caller()
            "
            );

            await Assert.That(result.Type).IsEqualTo(DataType.Boolean).ConfigureAwait(false);
            await Assert.That(result.Boolean).IsFalse().ConfigureAwait(false);
        }

        [global::TUnit.Core.Test]
        [LuaVersionsFrom(LuaCompatibilityVersion.Lua52)]
        public async Task DebugGetInfoReportsFalseForNonTailCalls(LuaCompatibilityVersion version)
        {
            Script script = new(version, CoreModules.Debug);
            DynValue result = script.DoString(
                @"
                local function target()
                    return debug.getinfo(1, 't').istailcall
                end

                local function caller()
                    local is_tail = target()
                    return is_tail
                end

                return caller()
            "
            );

            await Assert.That(result.Type).IsEqualTo(DataType.Boolean).ConfigureAwait(false);
            await Assert.That(result.Boolean).IsFalse().ConfigureAwait(false);
        }

        [global::TUnit.Core.Test]
        [LuaVersionsFrom(LuaCompatibilityVersion.Lua52)]
        public async Task DebugGetInfoFunctionTargetReportsFalseForTailCallFlag(
            LuaCompatibilityVersion version
        )
        {
            Script script = new(version, CoreModules.Debug);
            DynValue result = script.DoString(
                @"
                local function target()
                end

                return debug.getinfo(target, 't').istailcall
            "
            );

            await Assert.That(result.Type).IsEqualTo(DataType.Boolean).ConfigureAwait(false);
            await Assert.That(result.Boolean).IsFalse().ConfigureAwait(false);
        }

        [global::TUnit.Core.Test]
        public async Task DebugGetInfoRejectsTailCallOptionInLua51()
        {
            Script script = new(LuaCompatibilityVersion.Lua51, CoreModules.Debug);

            ScriptRuntimeException exception = Assert.Throws<ScriptRuntimeException>(() =>
                script.DoString(
                    @"
                    return debug.getinfo(1, 't')
                "
                )
            );

            await Assert
                .That(exception.DecoratedMessage)
                .Contains("invalid option")
                .ConfigureAwait(false);
        }

        [global::TUnit.Core.Test]
        public async Task DebugGetInfoDefaultOmitsTailCallFlagInLua51()
        {
            Script script = new(LuaCompatibilityVersion.Lua51, CoreModules.Debug);
            DynValue result = script.DoString(
                @"
                local info = debug.getinfo(1)
                return info.istailcall == nil
            "
            );

            await Assert.That(result.Type).IsEqualTo(DataType.Boolean).ConfigureAwait(false);
            await Assert.That(result.Boolean).IsTrue().ConfigureAwait(false);
        }

        [global::TUnit.Core.Test]
        [AllLuaVersions]
        public async Task TailCallPreservesCapturedUpvalues(LuaCompatibilityVersion version)
        {
            Script script = new(version);
            DynValue result = script.DoString(
                @"
                local saved

                local function finish()
                    return saved()
                end

                local function caller(value)
                    local captured = value
                    saved = function()
                        return captured
                    end

                    return finish()
                end

                return caller(91)
            "
            );

            await Assert.That(result.Type).IsEqualTo(DataType.Number).ConfigureAwait(false);
            await Assert.That(result.Number).IsEqualTo(91).ConfigureAwait(false);
        }

        [global::TUnit.Core.Test]
        [LuaVersionsFrom(LuaCompatibilityVersion.Lua52)]
        public async Task CallableTableTailCallReportsTailCallFrame(LuaCompatibilityVersion version)
        {
            Script script = new(
                version,
                CoreModules.Basic | CoreModules.Debug | CoreModules.Metatables
            );
            DynValue result = script.DoString(
                @"
                local callable = setmetatable({}, {
                    __call = function()
                        return debug.getinfo(1, 't').istailcall
                    end
                })

                local function caller()
                    return callable()
                end

                return caller()
            "
            );

            await Assert.That(result.Type).IsEqualTo(DataType.Boolean).ConfigureAwait(false);
            await Assert.That(result.Boolean).IsTrue().ConfigureAwait(false);
        }

        [global::TUnit.Core.Test]
        [LuaVersionsFrom(LuaCompatibilityVersion.Lua52)]
        public async Task PcallTargetTailCallReportsTailCallFrame(LuaCompatibilityVersion version)
        {
            Script script = new(version, CoreModulePresets.Complete);
            DynValue result = script.DoString(
                @"
                local function target()
                    return debug.getinfo(1, 't').istailcall
                end

                local ok, is_tail = pcall(function()
                    return target()
                end)

                return ok, is_tail
            "
            );

            await Assert.That(result.Type).IsEqualTo(DataType.Tuple).ConfigureAwait(false);
            await Assert.That(result.Tuple.Length).IsEqualTo(2).ConfigureAwait(false);
            await Assert.That(result.Tuple[0].Boolean).IsTrue().ConfigureAwait(false);
            await Assert.That(result.Tuple[1].Boolean).IsTrue().ConfigureAwait(false);
        }

        [global::TUnit.Core.Test]
        [LuaVersionsFrom(LuaCompatibilityVersion.Lua52)]
        public async Task XpcallTargetTailCallReportsTailCallFrame(LuaCompatibilityVersion version)
        {
            Script script = new(version, CoreModulePresets.Complete);
            DynValue result = script.DoString(
                @"
                local function target()
                    return debug.getinfo(1, 't').istailcall
                end

                local ok, is_tail = xpcall(function()
                    return target()
                end, function(message)
                    return message
                end)

                return ok, is_tail
            "
            );

            await Assert.That(result.Type).IsEqualTo(DataType.Tuple).ConfigureAwait(false);
            await Assert.That(result.Tuple.Length).IsEqualTo(2).ConfigureAwait(false);
            await Assert.That(result.Tuple[0].Boolean).IsTrue().ConfigureAwait(false);
            await Assert.That(result.Tuple[1].Boolean).IsTrue().ConfigureAwait(false);
        }

        [global::TUnit.Core.Test]
        [AllLuaVersions]
        public async Task XpcallTargetTailCallPreservesErrorHandler(LuaCompatibilityVersion version)
        {
            Script script = new(version, CoreModulePresets.Complete);
            DynValue result = script.DoString(
                @"
                local function target()
                    error('tail boom', 0)
                end

                return xpcall(function()
                    return target()
                end, function(message)
                    return 'handled:' .. message
                end)
            "
            );

            await Assert.That(result.Type).IsEqualTo(DataType.Tuple).ConfigureAwait(false);
            await Assert.That(result.Tuple.Length).IsEqualTo(2).ConfigureAwait(false);
            await Assert.That(result.Tuple[0].Boolean).IsFalse().ConfigureAwait(false);
            await Assert
                .That(result.Tuple[1].String)
                .IsEqualTo("handled:tail boom")
                .ConfigureAwait(false);
        }

        [global::TUnit.Core.Test]
        [AllLuaVersions]
        public async Task CallableTableTailRecursionDoesNotTripSandboxCallDepth(
            LuaCompatibilityVersion version
        )
        {
            ScriptOptions options = new()
            {
                CompatibilityVersion = version,
                Sandbox = new SandboxOptions(),
            };
            Script script = new(CoreModules.Basic | CoreModules.Metatables, options);
            script.DoString(
                @"
                local callable
                callable = setmetatable({}, {
                    __call = function(_, n, acc)
                        if n == 0 then
                            return acc
                        end

                        return callable(n - 1, acc + 1)
                    end
                })

                function run_callable_tail_recursion()
                    return callable(100, 0)
                end
            "
            );
            script.Options.Sandbox.MaxCallStackDepth = 1;

            DynValue result = script.Call(script.Globals.Get("run_callable_tail_recursion"));

            await Assert.That(result.Type).IsEqualTo(DataType.Number).ConfigureAwait(false);
            await Assert.That(result.Number).IsEqualTo(100).ConfigureAwait(false);
        }

        [global::TUnit.Core.Test]
        [AllLuaVersions]
        public async Task TailRecursionDoesNotTripSandboxCallDepth(LuaCompatibilityVersion version)
        {
            ScriptOptions options = new()
            {
                CompatibilityVersion = version,
                Sandbox = new SandboxOptions { MaxCallStackDepth = 5 },
            };
            Script script = new(options);
            DynValue result = script.DoString(
                @"
                local function recur(n, acc)
                    if n == 0 then
                        return acc
                    end

                    return recur(n - 1, acc + 1)
                end

                return recur(100, 0)
            "
            );

            await Assert.That(result.Type).IsEqualTo(DataType.Number).ConfigureAwait(false);
            await Assert.That(result.Number).IsEqualTo(100).ConfigureAwait(false);
        }

        [global::TUnit.Core.Test]
        [LuaVersionsFrom(LuaCompatibilityVersion.Lua54)]
        public async Task ToBeClosedTailPositionCallDoesNotSkipCloseHandlers(
            LuaCompatibilityVersion version
        )
        {
            Script script = new(version);
            DynValue result = script.DoString(
                @"
                local closed = 0
                local mt = {
                    __close = function()
                        closed = closed + 1
                    end
                }

                local function recur(n)
                    local handle <close> = setmetatable({}, mt)
                    if n == 0 then
                        return closed
                    end

                    return recur(n - 1)
                end

                local before = recur(70000)
                return before, closed
            "
            );

            await Assert.That(result.Type).IsEqualTo(DataType.Tuple).ConfigureAwait(false);
            await Assert.That(result.Tuple.Length).IsEqualTo(2).ConfigureAwait(false);
            await Assert.That(result.Tuple[0].Number).IsEqualTo(0).ConfigureAwait(false);
            await Assert.That(result.Tuple[1].Number).IsEqualTo(70001).ConfigureAwait(false);
        }

        [global::TUnit.Core.Test]
        [LuaVersionsFrom(LuaCompatibilityVersion.Lua54)]
        public async Task ToBeClosedTailPositionCallReportsNonTailFrame(
            LuaCompatibilityVersion version
        )
        {
            Script script = new(
                version,
                CoreModules.Basic | CoreModules.Debug | CoreModules.Metatables
            );
            DynValue result = script.DoString(
                @"
                local mt = {
                    __close = function()
                    end
                }

                local function target()
                    return debug.getinfo(1, 't').istailcall
                end

                local function caller()
                    local handle <close> = setmetatable({}, mt)
                    return target()
                end

                return caller()
            "
            );

            await Assert.That(result.Type).IsEqualTo(DataType.Boolean).ConfigureAwait(false);
            await Assert.That(result.Boolean).IsFalse().ConfigureAwait(false);
        }

        [global::TUnit.Core.Test]
        [LuaVersionsFrom(LuaCompatibilityVersion.Lua54)]
        public async Task NonCallableTailPositionWithCloseRunsCloseHandlerOnError(
            LuaCompatibilityVersion version
        )
        {
            Script script = new(version);
            DynValue result = script.DoString(
                @"
                local closed_with_error = false
                local mt = {
                    __close = function(_, err)
                        closed_with_error = err ~= nil
                    end
                }

                local function caller()
                    local handle <close> = setmetatable({}, mt)
                    local not_callable = {}
                    return not_callable()
                end

                local ok = pcall(caller)
                return ok, closed_with_error
            "
            );

            await Assert.That(result.Type).IsEqualTo(DataType.Tuple).ConfigureAwait(false);
            await Assert.That(result.Tuple.Length).IsEqualTo(2).ConfigureAwait(false);
            await Assert.That(result.Tuple[0].Boolean).IsFalse().ConfigureAwait(false);
            await Assert.That(result.Tuple[1].Boolean).IsTrue().ConfigureAwait(false);
        }

        [global::TUnit.Core.Test]
        [AllLuaVersions]
        public async Task TailCallRequestPropagatesAcrossClrCallback(
            LuaCompatibilityVersion version
        )
        {
            Script script = new(version);
            script.Globals.Set(
                "clrtail",
                DynValue.NewCallback(
                    (context, args) =>
                    {
                        DynValue function = script.Globals.Get("getResult");
                        DynValue adjusted = DynValue.NewNumber(args[0].Number / 3);
                        return DynValue.NewTailCallReq(function, adjusted);
                    }
                )
            );

            DynValue result = script.DoString(
                @"
                function getResult(x)
                    return 156 * x
                end

                return clrtail(9)
            "
            );

            await Assert.That(result.Type).IsEqualTo(DataType.Number).ConfigureAwait(false);
            await Assert.That(result.Number).IsEqualTo(468).ConfigureAwait(false);
        }

        [global::TUnit.Core.Test]
        [AllLuaVersions]
        public async Task ArgumentViewTailCallContinuationKeepsSpanBackedReturnValue(
            LuaCompatibilityVersion version
        )
        {
            Script script = new(version);
            bool spanAvailable = false;
            int spanLength = -1;
            double spanValue = 0d;

            CallbackFunction continuation = CallbackFunction.FromArgumentView(
                (_, args) =>
                {
                    spanAvailable = args.TryGetSpan(out ReadOnlySpan<DynValue> span);
                    spanLength = span.Length;
                    spanValue = spanAvailable && span.Length == 1 ? span[0].Number : -1d;
                    return DynValue.NewNumber(args[0].Number + 1d);
                },
                "span-continuation"
            );

            script.Globals.Set(
                "clrtail",
                DynValue.NewCallback(
                    (_, _) =>
                    {
                        DynValue function = script.Globals.Get("returnValue");
                        return DynValue.NewTailCallReq(
                            new TailCallData
                            {
                                Function = function,
                                Args = Array.Empty<DynValue>(),
                                Continuation = continuation,
                            }
                        );
                    }
                )
            );

            DynValue result = script.DoString(
                @"
                function returnValue()
                    return 41
                end

                return clrtail()
            "
            );

            await Assert.That(result.Type).IsEqualTo(DataType.Number).ConfigureAwait(false);
            await Assert.That(result.Number).IsEqualTo(42d).ConfigureAwait(false);
            await Assert.That(spanAvailable).IsTrue().ConfigureAwait(false);
            await Assert.That(spanLength).IsEqualTo(1).ConfigureAwait(false);
            await Assert.That(spanValue).IsEqualTo(41d).ConfigureAwait(false);
        }

        [global::TUnit.Core.Test]
        [AllLuaVersions]
        public async Task BasicModuleToStringConvertsNumbers(LuaCompatibilityVersion version)
        {
            Script script = new(version, CoreModules.Basic);
            DynValue result = script.DoString(
                @"
                return tostring(9)
            "
            );

            await Assert.That(result.Type).IsEqualTo(DataType.String).ConfigureAwait(false);
            await Assert.That(result.String).IsEqualTo("9").ConfigureAwait(false);
        }

        [global::TUnit.Core.Test]
        [AllLuaVersions]
        public async Task TostringUsesMetamethodsWhenAvailable(LuaCompatibilityVersion version)
        {
            Script script = new(version);
            DynValue result = script.DoString(
                @"
                local target = {}
                local meta = {
                    __tostring = function()
                        return 'ciao'
                    end
                }

                setmetatable(target, meta)
                return tostring(target)
            "
            );

            await Assert.That(result.Type).IsEqualTo(DataType.String).ConfigureAwait(false);
            await Assert.That(result.String).IsEqualTo("ciao").ConfigureAwait(false);
        }
    }
}
