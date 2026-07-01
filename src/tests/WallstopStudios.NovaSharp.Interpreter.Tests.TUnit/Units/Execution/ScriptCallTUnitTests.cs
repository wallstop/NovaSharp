namespace WallstopStudios.NovaSharp.Interpreter.Tests.TUnit.Units.Execution
{
    using System;
    using System.Collections.Generic;
    using System.Threading.Tasks;
    using global::TUnit.Assertions;
    using WallstopStudios.NovaSharp.Interpreter;
    using WallstopStudios.NovaSharp.Interpreter.Compatibility;
    using WallstopStudios.NovaSharp.Interpreter.DataTypes;
    using WallstopStudios.NovaSharp.Interpreter.Errors;
    using WallstopStudios.NovaSharp.Interpreter.Execution;
    using WallstopStudios.NovaSharp.Interpreter.Loaders;
    using WallstopStudios.NovaSharp.Interpreter.Modules;
    using WallstopStudios.NovaSharp.Tests.TestInfrastructure.TUnit;

    public sealed class ScriptCallTUnitTests
    {
        [global::TUnit.Core.Test]
        [global::TUnit.Core.Arguments(LuaCompatibilityVersion.Lua51)]
        [global::TUnit.Core.Arguments(LuaCompatibilityVersion.Lua52)]
        [global::TUnit.Core.Arguments(LuaCompatibilityVersion.Lua53)]
        [global::TUnit.Core.Arguments(LuaCompatibilityVersion.Lua54)]
        [global::TUnit.Core.Arguments(LuaCompatibilityVersion.Lua55)]
        public async Task CallWithNullDynValueArgsThrows(LuaCompatibilityVersion version)
        {
            Script script = new(version, CoreModulePresets.Complete);
            script.DoString("function noop() end");
            DynValue function = script.Globals.Get("noop");

            ArgumentNullException exception = Assert.Throws<ArgumentNullException>(() =>
                script.Call(function, (DynValue[])null)
            );

            await Assert.That(exception.ParamName).IsEqualTo("args").ConfigureAwait(false);
        }

        [global::TUnit.Core.Test]
        [global::TUnit.Core.Arguments(LuaCompatibilityVersion.Lua51)]
        [global::TUnit.Core.Arguments(LuaCompatibilityVersion.Lua52)]
        [global::TUnit.Core.Arguments(LuaCompatibilityVersion.Lua53)]
        [global::TUnit.Core.Arguments(LuaCompatibilityVersion.Lua54)]
        [global::TUnit.Core.Arguments(LuaCompatibilityVersion.Lua55)]
        public async Task CallWithNullObjectArgsThrows(LuaCompatibilityVersion version)
        {
            Script script = new(version, CoreModulePresets.Complete);
            script.DoString("function noop() end");
            DynValue function = script.Globals.Get("noop");

            ArgumentNullException exception = Assert.Throws<ArgumentNullException>(() =>
                script.Call(function, (object[])null)
            );

            await Assert.That(exception.ParamName).IsEqualTo("args").ConfigureAwait(false);
        }

        [global::TUnit.Core.Test]
        [global::TUnit.Core.Arguments(LuaCompatibilityVersion.Lua51)]
        [global::TUnit.Core.Arguments(LuaCompatibilityVersion.Lua52)]
        [global::TUnit.Core.Arguments(LuaCompatibilityVersion.Lua53)]
        [global::TUnit.Core.Arguments(LuaCompatibilityVersion.Lua54)]
        [global::TUnit.Core.Arguments(LuaCompatibilityVersion.Lua55)]
        public async Task CallWithNullFunctionThrows(LuaCompatibilityVersion version)
        {
            Script script = new(version, CoreModulePresets.Complete);

            ArgumentNullException exception = Assert.Throws<ArgumentNullException>(() =>
                script.Call((DynValue)null)
            );

            await Assert.That(exception.ParamName).IsEqualTo("function").ConfigureAwait(false);
        }

        [global::TUnit.Core.Test]
        [AllLuaVersions]
        public async Task DynValueCallInvokesZeroArgumentLuaFunction(
            LuaCompatibilityVersion version
        )
        {
            Script script = new(version, CoreModulePresets.Complete);
            DynValue function = script.DoString("return function() return 42 end");

            DynValue result = script.Call(function);

            await Assert.That(result.Number).IsEqualTo(42d).ConfigureAwait(false);
        }

        [global::TUnit.Core.Test]
        [AllLuaVersions]
        public async Task DynValueCallPreservesDebugFrameFunctionIdentity(
            LuaCompatibilityVersion version
        )
        {
            Script script = new(version, CoreModulePresets.Complete);
            DynValue function = script.DoString(
                """
                return function()
                    local info = debug.getinfo(1, "fS")
                    local funcInfo = debug.getinfo(info.func, "S")
                    local identity = info.func == expected and "same" or "different"
                    return identity .. ":" .. type(info.func) .. ":" .. info.what .. ":" .. funcInfo.short_src
                end
                """,
                codeFriendlyName: "call_debug.lua"
            );
            script.Globals.Set("expected", function);

            DynValue result = script.Call(function);

            await Assert
                .That(result.String)
                .IsEqualTo("same:function:Lua:call_debug.lua")
                .ConfigureAwait(false);
        }

        [global::TUnit.Core.Test]
        [AllLuaVersions]
        public async Task DynValueCallEmptyArgumentsUseZeroArgumentLuaFunction(
            LuaCompatibilityVersion version
        )
        {
            Script script = new(version, CoreModulePresets.Complete);
            DynValue function = script.DoString(
                """
                return function(...)
                    local info = debug.getinfo(1, "f")
                    local identity = info.func == expected and "same" or "different"
                    return select("#", ...), identity
                end
                """,
                codeFriendlyName: "call_empty_span.lua"
            );
            script.Globals.Set("expected", function);

            DynValue spanResult = CallWithSpan(script, function, Array.Empty<DynValue>());
            DynValue paramsResult = CallWithParamsArray(script, function, Array.Empty<DynValue>());

            await Assert.That(spanResult.Tuple[0].Number).IsEqualTo(0d).ConfigureAwait(false);
            await Assert.That(spanResult.Tuple[1].String).IsEqualTo("same").ConfigureAwait(false);
            await Assert.That(paramsResult.Tuple[0].Number).IsEqualTo(0d).ConfigureAwait(false);
            await Assert.That(paramsResult.Tuple[1].String).IsEqualTo("same").ConfigureAwait(false);
        }

        [global::TUnit.Core.Test]
        [LuaVersionsUntil(LuaCompatibilityVersion.Lua51)]
        public async Task DynValueCallSupportsLua51SetfenvFrame(LuaCompatibilityVersion version)
        {
            Script script = new(version, CoreModulePresets.Complete);
            script.Globals.Set("marker", DynValue.FromNumber(5));
            DynValue function = script.DoString(
                """
                return function()
                    local before = getfenv(1).marker
                    setfenv(1, { marker = 99, getfenv = getfenv, setfenv = setfenv })
                    return before * 100 + getfenv(1).marker
                end
                """,
                codeFriendlyName: "call_setfenv.lua"
            );

            DynValue result = script.Call(function);

            await Assert.That(result.Number).IsEqualTo(599d).ConfigureAwait(false);
        }

        [global::TUnit.Core.Test]
        [global::TUnit.Core.Arguments(LuaCompatibilityVersion.Lua51)]
        [global::TUnit.Core.Arguments(LuaCompatibilityVersion.Lua52)]
        [global::TUnit.Core.Arguments(LuaCompatibilityVersion.Lua53)]
        [global::TUnit.Core.Arguments(LuaCompatibilityVersion.Lua54)]
        [global::TUnit.Core.Arguments(LuaCompatibilityVersion.Lua55)]
        public async Task CallInvokesMetamethodWhenValueHasCall(LuaCompatibilityVersion version)
        {
            Script script = new(version, CoreModulePresets.Complete);
            script.DoString(
                @"
                local mt = {}
                function mt:__call(value)
                    return value * 2
                end
                callable = setmetatable({}, mt)
            "
            );

            DynValue callable = script.Globals.Get("callable");
            DynValue result = script.Call(callable, DynValue.NewNumber(21));

            await Assert.That(result.Number).IsEqualTo(42d).ConfigureAwait(false);
        }

        [global::TUnit.Core.Test]
        [global::TUnit.Core.Arguments(LuaCompatibilityVersion.Lua51)]
        [global::TUnit.Core.Arguments(LuaCompatibilityVersion.Lua52)]
        [global::TUnit.Core.Arguments(LuaCompatibilityVersion.Lua53)]
        [global::TUnit.Core.Arguments(LuaCompatibilityVersion.Lua54)]
        [global::TUnit.Core.Arguments(LuaCompatibilityVersion.Lua55)]
        public async Task FourDynValueCallInvokesMetamethodWhenValueHasCall(
            LuaCompatibilityVersion version
        )
        {
            Script script = new(version, CoreModulePresets.Complete);
            script.DoString(
                @"
                local mt = {}
                function mt:__call(a, b, c, d)
                    return a + b + c + d
                end
                callable = setmetatable({}, mt)
            "
            );

            DynValue callable = script.Globals.Get("callable");
            DynValue result = script.Call(
                callable,
                DynValue.NewNumber(10),
                DynValue.NewNumber(20),
                DynValue.NewNumber(30),
                DynValue.NewNumber(40)
            );

            await Assert.That(result.Number).IsEqualTo(100d).ConfigureAwait(false);
        }

        [global::TUnit.Core.Test]
        [global::TUnit.Core.Arguments(LuaCompatibilityVersion.Lua51)]
        [global::TUnit.Core.Arguments(LuaCompatibilityVersion.Lua52)]
        [global::TUnit.Core.Arguments(LuaCompatibilityVersion.Lua53)]
        [global::TUnit.Core.Arguments(LuaCompatibilityVersion.Lua54)]
        [global::TUnit.Core.Arguments(LuaCompatibilityVersion.Lua55)]
        public async Task FiveDynValueCallInvokesLuaFunction(LuaCompatibilityVersion version)
        {
            Script script = new(version, CoreModulePresets.Complete);
            DynValue function = script.DoString(
                "return function(a, b, c, d, e) return a + b + c + d + e end"
            );

            DynValue result = script.Call(
                function,
                DynValue.NewNumber(1),
                DynValue.NewNumber(2),
                DynValue.NewNumber(3),
                DynValue.NewNumber(4),
                DynValue.NewNumber(5)
            );

            await Assert.That(result.Number).IsEqualTo(15d).ConfigureAwait(false);
        }

        [global::TUnit.Core.Test]
        [AllLuaVersions]
        public async Task FiveDynValueCallInvokesMetamethodWhenValueHasCall(
            LuaCompatibilityVersion version
        )
        {
            Script script = new(version, CoreModulePresets.Complete);
            script.DoString(
                @"
                local mt = {}
                function mt:__call(a, b, c, d, e)
                    return self.marker + a + b + c + d + e
                end
                callable = setmetatable({ marker = 100 }, mt)
            "
            );

            DynValue callable = script.Globals.Get("callable");
            DynValue result = script.Call(
                callable,
                DynValue.NewNumber(1),
                DynValue.NewNumber(2),
                DynValue.NewNumber(3),
                DynValue.NewNumber(4),
                DynValue.NewNumber(5)
            );

            await Assert.That(result.Number).IsEqualTo(115d).ConfigureAwait(false);
        }

        [global::TUnit.Core.Test]
        [global::TUnit.Core.Arguments(LuaCompatibilityVersion.Lua51)]
        [global::TUnit.Core.Arguments(LuaCompatibilityVersion.Lua52)]
        [global::TUnit.Core.Arguments(LuaCompatibilityVersion.Lua53)]
        [global::TUnit.Core.Arguments(LuaCompatibilityVersion.Lua54)]
        [global::TUnit.Core.Arguments(LuaCompatibilityVersion.Lua55)]
        public async Task CallExecutesClrFunction(LuaCompatibilityVersion version)
        {
            Script script = new(version, CoreModulePresets.Complete);
            DynValue callback = DynValue.NewCallback((_, _) => DynValue.NewString("clr"));

            DynValue result = script.Call(callback);

            await Assert.That(result.Type).IsEqualTo(DataType.String).ConfigureAwait(false);
            await Assert.That(result.String).IsEqualTo("clr").ConfigureAwait(false);
        }

        [global::TUnit.Core.Test]
        [global::TUnit.Core.Arguments(LuaCompatibilityVersion.Lua51)]
        [global::TUnit.Core.Arguments(LuaCompatibilityVersion.Lua52)]
        [global::TUnit.Core.Arguments(LuaCompatibilityVersion.Lua53)]
        [global::TUnit.Core.Arguments(LuaCompatibilityVersion.Lua54)]
        [global::TUnit.Core.Arguments(LuaCompatibilityVersion.Lua55)]
        public async Task FourDynValueCallExecutesClrFunction(LuaCompatibilityVersion version)
        {
            Script script = new(version, CoreModulePresets.Complete);
            DynValue callback = DynValue.NewCallback(
                (_, args) =>
                    DynValue.NewNumber(
                        args.Count
                            + args[0].Number
                            + args[1].Number
                            + args[2].Number
                            + args[3].Number
                    )
            );

            DynValue result = script.Call(
                callback,
                DynValue.NewNumber(10),
                DynValue.NewNumber(20),
                DynValue.NewNumber(30),
                DynValue.NewNumber(40)
            );

            await Assert.That(result.Number).IsEqualTo(104d).ConfigureAwait(false);
        }

        [global::TUnit.Core.Test]
        [global::TUnit.Core.Arguments(LuaCompatibilityVersion.Lua51)]
        [global::TUnit.Core.Arguments(LuaCompatibilityVersion.Lua52)]
        [global::TUnit.Core.Arguments(LuaCompatibilityVersion.Lua53)]
        [global::TUnit.Core.Arguments(LuaCompatibilityVersion.Lua54)]
        [global::TUnit.Core.Arguments(LuaCompatibilityVersion.Lua55)]
        public async Task FiveDynValueCallExecutesClrFunction(LuaCompatibilityVersion version)
        {
            Script script = new(version, CoreModulePresets.Complete);
            DynValue callback = DynValue.NewCallback(
                (_, args) =>
                    DynValue.NewNumber(
                        args.Count
                            + args[0].Number
                            + args[1].Number
                            + args[2].Number
                            + args[3].Number
                            + args[4].Number
                    )
            );

            DynValue result = script.Call(
                callback,
                DynValue.NewNumber(10),
                DynValue.NewNumber(20),
                DynValue.NewNumber(30),
                DynValue.NewNumber(40),
                DynValue.NewNumber(50)
            );

            await Assert.That(result.Number).IsEqualTo(155d).ConfigureAwait(false);
        }

        [global::TUnit.Core.Test]
        [global::TUnit.Core.Arguments(LuaCompatibilityVersion.Lua51)]
        [global::TUnit.Core.Arguments(LuaCompatibilityVersion.Lua52)]
        [global::TUnit.Core.Arguments(LuaCompatibilityVersion.Lua53)]
        [global::TUnit.Core.Arguments(LuaCompatibilityVersion.Lua54)]
        [global::TUnit.Core.Arguments(LuaCompatibilityVersion.Lua55)]
        public async Task FourDynValueCallExecutesCallbackView(LuaCompatibilityVersion version)
        {
            Script script = new(version, CoreModulePresets.Complete);
            DynValue callback = DynValue.NewCallbackView(
                (_, args) =>
                    DynValue.NewNumber(
                        args.Count
                            + args[0].Number
                            + args[1].Number
                            + args[2].Number
                            + args[3].Number
                    )
            );

            DynValue result = script.Call(
                callback,
                DynValue.NewNumber(10),
                DynValue.NewNumber(20),
                DynValue.NewNumber(30),
                DynValue.NewNumber(40)
            );

            await Assert.That(result.Number).IsEqualTo(104d).ConfigureAwait(false);
        }

        [global::TUnit.Core.Test]
        [global::TUnit.Core.Arguments(LuaCompatibilityVersion.Lua51)]
        [global::TUnit.Core.Arguments(LuaCompatibilityVersion.Lua52)]
        [global::TUnit.Core.Arguments(LuaCompatibilityVersion.Lua53)]
        [global::TUnit.Core.Arguments(LuaCompatibilityVersion.Lua54)]
        [global::TUnit.Core.Arguments(LuaCompatibilityVersion.Lua55)]
        public async Task FiveDynValueCallExecutesCallbackView(LuaCompatibilityVersion version)
        {
            Script script = new(version, CoreModulePresets.Complete);
            bool spanAvailable = true;
            int spanLength = -1;
            DynValue callback = DynValue.NewCallbackView(
                (_, args) =>
                {
                    spanAvailable = args.TryGetSpan(out ReadOnlySpan<DynValue> span);
                    spanLength = span.Length;
                    return DynValue.NewNumber(
                        args.Count
                            + args[0].Number
                            + args[1].Number
                            + args[2].Number
                            + args[3].Number
                            + args[4].Number
                    );
                }
            );

            DynValue result = script.Call(
                callback,
                DynValue.NewNumber(10),
                DynValue.NewNumber(20),
                DynValue.NewNumber(30),
                DynValue.NewNumber(40),
                DynValue.NewNumber(50)
            );

            await Assert.That(result.Number).IsEqualTo(155d).ConfigureAwait(false);
            await Assert.That(spanAvailable).IsFalse().ConfigureAwait(false);
            await Assert.That(spanLength).IsEqualTo(0).ConfigureAwait(false);
        }

        [global::TUnit.Core.Test]
        [global::TUnit.Core.Arguments(LuaCompatibilityVersion.Lua51)]
        [global::TUnit.Core.Arguments(LuaCompatibilityVersion.Lua52)]
        [global::TUnit.Core.Arguments(LuaCompatibilityVersion.Lua53)]
        [global::TUnit.Core.Arguments(LuaCompatibilityVersion.Lua54)]
        [global::TUnit.Core.Arguments(LuaCompatibilityVersion.Lua55)]
        public async Task FixedDynValueCallToCallbackViewExpandsTrailingTuple(
            LuaCompatibilityVersion version
        )
        {
            Script script = new(version, CoreModulePresets.Complete);
            DynValue callback = DynValue.NewCallbackView(
                (_, args) =>
                    DynValue.NewNumber(
                        args.Count + args[0].Number + args[1].Number + args[2].Number
                    )
            );

            DynValue result = script.Call(
                callback,
                DynValue.NewNumber(10),
                DynValue.NewTuple(DynValue.NewNumber(20), DynValue.NewNumber(30))
            );

            await Assert.That(result.Number).IsEqualTo(63d).ConfigureAwait(false);
        }

        [global::TUnit.Core.Test]
        [global::TUnit.Core.Arguments(LuaCompatibilityVersion.Lua51)]
        [global::TUnit.Core.Arguments(LuaCompatibilityVersion.Lua52)]
        [global::TUnit.Core.Arguments(LuaCompatibilityVersion.Lua53)]
        [global::TUnit.Core.Arguments(LuaCompatibilityVersion.Lua54)]
        [global::TUnit.Core.Arguments(LuaCompatibilityVersion.Lua55)]
        public async Task FixedDynValueCallToCallbackViewIgnoresTrailingVoid(
            LuaCompatibilityVersion version
        )
        {
            Script script = new(version, CoreModulePresets.Complete);
            DynValue callback = DynValue.NewCallbackView(
                (_, args) => DynValue.NewNumber(args.Count + args[0].Number)
            );

            DynValue result = script.Call(callback, DynValue.NewNumber(10), DynValue.Void);

            await Assert.That(result.Number).IsEqualTo(11d).ConfigureAwait(false);
        }

        [global::TUnit.Core.Test]
        [global::TUnit.Core.Arguments(LuaCompatibilityVersion.Lua51)]
        [global::TUnit.Core.Arguments(LuaCompatibilityVersion.Lua52)]
        [global::TUnit.Core.Arguments(LuaCompatibilityVersion.Lua53)]
        [global::TUnit.Core.Arguments(LuaCompatibilityVersion.Lua54)]
        [global::TUnit.Core.Arguments(LuaCompatibilityVersion.Lua55)]
        public async Task LuaCallToCallbackViewExposesContiguousSpan(
            LuaCompatibilityVersion version
        )
        {
            Script script = new(version, CoreModulePresets.Complete);
            bool spanAvailable = false;
            int spanLength = -1;
            double first = -1;
            double third = -1;

            script.Globals["callback"] = DynValue.NewCallbackView(
                (_, args) =>
                {
                    spanAvailable = args.TryGetSpan(out ReadOnlySpan<DynValue> span);
                    spanLength = span.Length;
                    if (spanAvailable)
                    {
                        first = span[0].Number;
                        third = span[2].Number;
                    }

                    return DynValue.NewNumber(args.Count);
                }
            );

            DynValue result = script.DoString("return callback(10, 20, 30)");

            await Assert.That(result.Number).IsEqualTo(3d).ConfigureAwait(false);
            await Assert.That(spanAvailable).IsTrue().ConfigureAwait(false);
            await Assert.That(spanLength).IsEqualTo(3).ConfigureAwait(false);
            await Assert.That(first).IsEqualTo(10d).ConfigureAwait(false);
            await Assert.That(third).IsEqualTo(30d).ConfigureAwait(false);
        }

        [global::TUnit.Core.Test]
        [global::TUnit.Core.Arguments(LuaCompatibilityVersion.Lua51)]
        [global::TUnit.Core.Arguments(LuaCompatibilityVersion.Lua52)]
        [global::TUnit.Core.Arguments(LuaCompatibilityVersion.Lua53)]
        [global::TUnit.Core.Arguments(LuaCompatibilityVersion.Lua54)]
        [global::TUnit.Core.Arguments(LuaCompatibilityVersion.Lua55)]
        public async Task LuaCallToCallbackViewHandlesZeroAndManyArguments(
            LuaCompatibilityVersion version
        )
        {
            Script script = new(version, CoreModulePresets.Complete);
            int zeroCount = -1;
            int manyCount = -1;
            bool manySpanAvailable = false;
            int manySpanLength = -1;

            script.Globals["callback"] = DynValue.NewCallbackView(
                (_, args) =>
                {
                    if (args.Count == 0)
                    {
                        zeroCount = args.Count;
                        return DynValue.NewNumber(0);
                    }

                    manyCount = args.Count;
                    manySpanAvailable = args.TryGetSpan(out ReadOnlySpan<DynValue> span);
                    manySpanLength = span.Length;
                    return DynValue.NewNumber(args[4].Number + args.Count);
                }
            );

            DynValue result = script.DoString("callback(); return callback(1, 2, 3, 4, 5)");

            await Assert.That(result.Number).IsEqualTo(10d).ConfigureAwait(false);
            await Assert.That(zeroCount).IsEqualTo(0).ConfigureAwait(false);
            await Assert.That(manyCount).IsEqualTo(5).ConfigureAwait(false);
            await Assert.That(manySpanAvailable).IsTrue().ConfigureAwait(false);
            await Assert.That(manySpanLength).IsEqualTo(5).ConfigureAwait(false);
        }

        [global::TUnit.Core.Test]
        [global::TUnit.Core.Arguments(LuaCompatibilityVersion.Lua51)]
        [global::TUnit.Core.Arguments(LuaCompatibilityVersion.Lua52)]
        [global::TUnit.Core.Arguments(LuaCompatibilityVersion.Lua53)]
        [global::TUnit.Core.Arguments(LuaCompatibilityVersion.Lua54)]
        [global::TUnit.Core.Arguments(LuaCompatibilityVersion.Lua55)]
        public async Task LuaCallToCallbackViewExpandsTrailingTuple(LuaCompatibilityVersion version)
        {
            Script script = new(version, CoreModulePresets.Complete);
            bool spanAvailable = true;

            script.Globals["callback"] = DynValue.NewCallbackView(
                (_, args) =>
                {
                    spanAvailable = args.TryGetSpan(out ReadOnlySpan<DynValue> ignored);
                    spanAvailable = spanAvailable && ignored.Length >= 0;
                    return DynValue.NewNumber(
                        args.Count + args[0].Number + args[1].Number + args[2].Number
                    );
                }
            );

            DynValue result = script.DoString(
                @"
                local function values()
                    return 20, 30
                end

                return callback(10, values())
            "
            );

            await Assert.That(result.Number).IsEqualTo(63d).ConfigureAwait(false);
            await Assert.That(spanAvailable).IsFalse().ConfigureAwait(false);
        }

        [global::TUnit.Core.Test]
        [global::TUnit.Core.Arguments(LuaCompatibilityVersion.Lua51)]
        [global::TUnit.Core.Arguments(LuaCompatibilityVersion.Lua52)]
        [global::TUnit.Core.Arguments(LuaCompatibilityVersion.Lua53)]
        [global::TUnit.Core.Arguments(LuaCompatibilityVersion.Lua54)]
        [global::TUnit.Core.Arguments(LuaCompatibilityVersion.Lua55)]
        public async Task LuaCallToCallbackViewHandlesLuaNoReturnTrailingArgument(
            LuaCompatibilityVersion version
        )
        {
            Script script = new(version, CoreModulePresets.Complete);
            bool spanAvailable = true;
            int spanLength = -1;

            script.Globals["callback"] = DynValue.NewCallbackView(
                (_, args) =>
                {
                    spanAvailable = args.TryGetSpan(out ReadOnlySpan<DynValue> span);
                    spanLength = span.Length;
                    return DynValue.NewNumber(args.Count + args[0].Number);
                }
            );

            DynValue result = script.DoString(
                @"
                local function values()
                end

                return callback(10, values())
            "
            );

            await Assert.That(result.Number).IsEqualTo(11d).ConfigureAwait(false);
            await Assert.That(spanAvailable).IsTrue().ConfigureAwait(false);
            await Assert.That(spanLength).IsEqualTo(1).ConfigureAwait(false);
        }

        [global::TUnit.Core.Test]
        [global::TUnit.Core.Arguments(LuaCompatibilityVersion.Lua51)]
        [global::TUnit.Core.Arguments(LuaCompatibilityVersion.Lua52)]
        [global::TUnit.Core.Arguments(LuaCompatibilityVersion.Lua53)]
        [global::TUnit.Core.Arguments(LuaCompatibilityVersion.Lua54)]
        [global::TUnit.Core.Arguments(LuaCompatibilityVersion.Lua55)]
        public async Task LuaCallToCallbackViewHandlesLuaSingleReturnTrailingArgument(
            LuaCompatibilityVersion version
        )
        {
            Script script = new(version, CoreModulePresets.Complete);
            bool spanAvailable = true;
            int spanLength = -1;

            script.Globals["callback"] = DynValue.NewCallbackView(
                (_, args) =>
                {
                    spanAvailable = args.TryGetSpan(out ReadOnlySpan<DynValue> span);
                    spanLength = span.Length;
                    return DynValue.NewNumber(args.Count + args[0].Number + args[1].Number);
                }
            );

            DynValue result = script.DoString(
                @"
                local function values()
                    return 20
                end

                return callback(10, values())
            "
            );

            await Assert.That(result.Number).IsEqualTo(32d).ConfigureAwait(false);
            await Assert.That(spanAvailable).IsTrue().ConfigureAwait(false);
            await Assert.That(spanLength).IsEqualTo(2).ConfigureAwait(false);
        }

        [global::TUnit.Core.Test]
        [global::TUnit.Core.Arguments(LuaCompatibilityVersion.Lua51)]
        [global::TUnit.Core.Arguments(LuaCompatibilityVersion.Lua52)]
        [global::TUnit.Core.Arguments(LuaCompatibilityVersion.Lua53)]
        [global::TUnit.Core.Arguments(LuaCompatibilityVersion.Lua54)]
        [global::TUnit.Core.Arguments(LuaCompatibilityVersion.Lua55)]
        public async Task LuaCallToCallbackViewHandlesClrEmptyTrailingTuple(
            LuaCompatibilityVersion version
        )
        {
            Script script = new(version, CoreModulePresets.Complete);
            bool spanAvailable = true;

            script.Globals["empty"] = DynValue.NewCallbackView((_, _) => DynValue.EmptyTuple);
            script.Globals["callback"] = DynValue.NewCallbackView(
                (_, args) =>
                {
                    spanAvailable = args.TryGetSpan(out ReadOnlySpan<DynValue> span);
                    spanAvailable = spanAvailable && span.Length >= 0;
                    return DynValue.NewNumber(args.Count + args[0].Number);
                }
            );

            DynValue result = script.DoString("return callback(10, empty())");

            await Assert.That(result.Number).IsEqualTo(11d).ConfigureAwait(false);
            await Assert.That(spanAvailable).IsFalse().ConfigureAwait(false);
        }

        [global::TUnit.Core.Test]
        [global::TUnit.Core.Arguments(LuaCompatibilityVersion.Lua51)]
        [global::TUnit.Core.Arguments(LuaCompatibilityVersion.Lua52)]
        [global::TUnit.Core.Arguments(LuaCompatibilityVersion.Lua53)]
        [global::TUnit.Core.Arguments(LuaCompatibilityVersion.Lua54)]
        [global::TUnit.Core.Arguments(LuaCompatibilityVersion.Lua55)]
        public async Task LuaCallToCallbackViewScalarizesNonFinalTuple(
            LuaCompatibilityVersion version
        )
        {
            Script script = new(version, CoreModulePresets.Complete);

            script.Globals["callback"] = DynValue.NewCallbackView(
                (_, args) => DynValue.NewNumber(args.Count + args[0].Number + args[1].Number)
            );

            DynValue result = script.DoString(
                @"
                local function values()
                    return 10, 20
                end

                return callback(values(), 30)
            "
            );

            await Assert.That(result.Number).IsEqualTo(42d).ConfigureAwait(false);
        }

        [global::TUnit.Core.Test]
        [AllLuaVersions]
        public async Task LuaCallToLegacyClrFunctionPreservesFixedArgumentOrder(
            LuaCompatibilityVersion version
        )
        {
            Script script = new(version, CoreModulePresets.Complete);
            bool spanAvailable = false;
            int spanLength = -1;
            double first = -1d;
            double seventh = -1d;

            script.Globals["callback"] = DynValue.NewCallback(
                (_, args) =>
                {
                    spanAvailable = args.TryGetSpan(out ReadOnlySpan<DynValue> span);
                    spanLength = span.Length;
                    first = args[0].Number;
                    seventh = args[6].Number;
                    return DynValue.NewNumber(args.Count + first + seventh);
                }
            );

            DynValue result = script.DoString("return callback(1, 2, 3, 4, 5, 6, 7)");

            await Assert.That(result.Number).IsEqualTo(15d).ConfigureAwait(false);
            await Assert.That(spanAvailable).IsTrue().ConfigureAwait(false);
            await Assert.That(spanLength).IsEqualTo(7).ConfigureAwait(false);
            await Assert.That(first).IsEqualTo(1d).ConfigureAwait(false);
            await Assert.That(seventh).IsEqualTo(7d).ConfigureAwait(false);
        }

        [global::TUnit.Core.Test]
        [AllLuaVersions]
        public async Task LuaCallToLegacyClrFunctionPreservesTrailingTupleExpansion(
            LuaCompatibilityVersion version
        )
        {
            Script script = new(version, CoreModulePresets.Complete);
            script.Globals["callback"] = DynValue.NewCallback(
                (_, args) => SummarizeArguments(args)
            );

            DynValue tupleResult = script.DoString(
                @"
                local function values()
                    return 20, 30
                end

                return callback(10, values())
            "
            );

            await AssertArgumentSummary(tupleResult, count: 3d, nilCount: 0d, sum: 60d)
                .ConfigureAwait(false);

            DynValue noReturnResult = script.DoString(
                @"
                local function values()
                end

                return callback(10, values())
            "
            );

            await AssertArgumentSummary(noReturnResult, count: 1d, nilCount: 0d, sum: 10d)
                .ConfigureAwait(false);
        }

        [global::TUnit.Core.Test]
        [AllLuaVersions]
        public async Task LuaCallToLegacyClrFunctionPreservesClrTupleEdgeExpansion(
            LuaCompatibilityVersion version
        )
        {
            Script script = new(version, CoreModulePresets.Complete);
            script.Globals["callback"] = DynValue.NewCallback(
                (_, args) => SummarizeArguments(args)
            );
            script.Globals["voidTuple"] = DynValue.NewCallback(
                (_, _) => DynValue.NewTuple(DynValue.NewNumber(20), DynValue.Void)
            );
            script.Globals["nestedTuple"] = DynValue.NewCallback(
                (_, _) =>
                    DynValue.NewTuple(
                        DynValue.NewNumber(20),
                        DynValue.NewTuple(DynValue.NewNumber(30), DynValue.NewNumber(40))
                    )
            );

            DynValue voidResult = script.DoString("return callback(10, voidTuple())");
            await AssertArgumentSummary(voidResult, count: 2d, nilCount: 0d, sum: 30d)
                .ConfigureAwait(false);

            DynValue nestedResult = script.DoString("return callback(10, nestedTuple())");
            await AssertArgumentSummary(nestedResult, count: 4d, nilCount: 0d, sum: 100d)
                .ConfigureAwait(false);
        }

        [global::TUnit.Core.Test]
        [global::TUnit.Core.Arguments(0)]
        [global::TUnit.Core.Arguments(1)]
        [global::TUnit.Core.Arguments(2)]
        [global::TUnit.Core.Arguments(3)]
        [global::TUnit.Core.Arguments(4)]
        [global::TUnit.Core.Arguments(5)]
        [global::TUnit.Core.Arguments(6)]
        [global::TUnit.Core.Arguments(7)]
        public async Task FixedDynValueCallToLegacyClrFunctionExposesFixedStorageSpan(int arity)
        {
            Script script = new(CoreModulePresets.Complete);
            DynValue callback = DynValue.NewCallback(
                (_, args) =>
                {
                    bool success = args.TryGetSpan(out ReadOnlySpan<DynValue> span);
                    DynValue[] copied = new DynValue[args.Count];
                    int copiedCount = args.CopyTo(copied);
                    double sum = 0d;
                    for (int i = 0; i < copiedCount; i++)
                    {
                        sum += copied[i].Number;
                    }

                    return DynValue.NewTuple(
                        DynValue.NewNumber(args.Count),
                        DynValue.NewBoolean(success),
                        DynValue.NewNumber(span.Length),
                        DynValue.NewNumber(copiedCount),
                        DynValue.NewNumber(sum)
                    );
                }
            );

            DynValue result = CallLegacyCallbackWithSequentialArguments(script, callback, arity);

            await Assert.That(result.Type).IsEqualTo(DataType.Tuple).ConfigureAwait(false);
            await Assert
                .That(result.Tuple[0].Number)
                .IsEqualTo((double)arity)
                .ConfigureAwait(false);
            await Assert.That(result.Tuple[1].Boolean).IsTrue().ConfigureAwait(false);
            await Assert
                .That(result.Tuple[2].Number)
                .IsEqualTo((double)arity)
                .ConfigureAwait(false);
            await Assert
                .That(result.Tuple[3].Number)
                .IsEqualTo((double)arity)
                .ConfigureAwait(false);
            await Assert
                .That(result.Tuple[4].Number)
                .IsEqualTo(arity * (arity + 1) / 2d)
                .ConfigureAwait(false);
        }

        [global::TUnit.Core.Test]
        [global::TUnit.Core.Arguments(1)]
        [global::TUnit.Core.Arguments(2)]
        [global::TUnit.Core.Arguments(3)]
        [global::TUnit.Core.Arguments(4)]
        [global::TUnit.Core.Arguments(5)]
        [global::TUnit.Core.Arguments(6)]
        [global::TUnit.Core.Arguments(7)]
        public async Task FixedDynValueCallToLegacyClrFunctionPreservesArity(int arity)
        {
            Script script = new(CoreModulePresets.Complete);
            DynValue inspect = DynValue.NewCallback(
                (_, args) =>
                {
                    double sum = 0d;
                    for (int i = 0; i < args.Count; i++)
                    {
                        sum += args[i].Number;
                    }

                    return DynValue.NewTuple(
                        DynValue.NewNumber(args.Count),
                        DynValue.NewNumber(sum)
                    );
                }
            );

            DynValue result = CallLegacyCallbackWithSequentialArguments(script, inspect, arity);

            await Assert.That(result.Type).IsEqualTo(DataType.Tuple).ConfigureAwait(false);
            await Assert
                .That(result.Tuple[0].Number)
                .IsEqualTo((double)arity)
                .ConfigureAwait(false);
            await Assert
                .That(result.Tuple[1].Number)
                .IsEqualTo(arity * (arity + 1) / 2d)
                .ConfigureAwait(false);
        }

        [global::TUnit.Core.Test]
        [global::TUnit.Core.Arguments(1)]
        [global::TUnit.Core.Arguments(2)]
        [global::TUnit.Core.Arguments(3)]
        [global::TUnit.Core.Arguments(4)]
        [global::TUnit.Core.Arguments(5)]
        public async Task FixedDynValueCallToLegacyClrFunctionPreservesSpecialArguments(int arity)
        {
            Script script = new(CoreModulePresets.Complete);
            DynValue inspect = DynValue.NewCallback((_, args) => SummarizeArguments(args));

            DynValue result = arity switch
            {
                1 => script.Call(inspect, (DynValue)null),
                2 => script.Call(inspect, DynValue.NewNumber(1), DynValue.Void),
                3 => script.Call(
                    inspect,
                    DynValue.NewNumber(1),
                    DynValue.NewTuple(DynValue.NewNumber(2), DynValue.NewNumber(20)),
                    DynValue.NewTuple(DynValue.NewNumber(3), null)
                ),
                4 => script.Call(
                    inspect,
                    null,
                    DynValue.NewNumber(2),
                    DynValue.NewNumber(3),
                    DynValue.NewTuple(DynValue.NewNumber(4), null)
                ),
                5 => script.Call(
                    inspect,
                    DynValue.NewNumber(1),
                    null,
                    DynValue.NewTuple(DynValue.NewNumber(2), DynValue.NewNumber(20)),
                    DynValue.NewNumber(3),
                    DynValue.NewTuple(DynValue.NewNumber(4), null)
                ),
                _ => throw new ArgumentOutOfRangeException(nameof(arity)),
            };

            double expectedCount = arity switch
            {
                1 => 1d,
                2 => 1d,
                3 => 4d,
                4 => 5d,
                5 => 6d,
                _ => throw new ArgumentOutOfRangeException(nameof(arity)),
            };
            double expectedNilCount = arity switch
            {
                1 => 1d,
                2 => 0d,
                3 => 1d,
                4 => 2d,
                5 => 2d,
                _ => throw new ArgumentOutOfRangeException(nameof(arity)),
            };
            double expectedSum = arity switch
            {
                1 => 0d,
                2 => 1d,
                3 => 6d,
                4 => 9d,
                5 => 10d,
                _ => throw new ArgumentOutOfRangeException(nameof(arity)),
            };

            await Assert.That(result.Type).IsEqualTo(DataType.Tuple).ConfigureAwait(false);
            await Assert
                .That(result.Tuple[0].Number)
                .IsEqualTo(expectedCount)
                .ConfigureAwait(false);
            await Assert
                .That(result.Tuple[1].Number)
                .IsEqualTo(expectedNilCount)
                .ConfigureAwait(false);
            await Assert.That(result.Tuple[2].Number).IsEqualTo(expectedSum).ConfigureAwait(false);
        }

        [global::TUnit.Core.Test]
        public async Task FixedDynValueCallToLegacyClrFunctionDoesNotExposeSpanWhenArgumentsNeedNormalization()
        {
            Script script = new(CoreModulePresets.Complete);
            DynValue inspect = DynValue.NewCallback(
                (_, args) =>
                {
                    bool spanAvailable = args.TryGetSpan(out ReadOnlySpan<DynValue> span);
                    return DynValue.NewTuple(
                        DynValue.NewBoolean(spanAvailable),
                        DynValue.NewNumber(span.Length),
                        DynValue.NewNumber(args.Count),
                        args[0]
                    );
                }
            );

            DynValue nonFinalTuple = script.Call(
                inspect,
                DynValue.NewTuple(DynValue.NewNumber(1), DynValue.NewNumber(10)),
                DynValue.NewNumber(2)
            );
            DynValue nonFinalVoid = script.Call(inspect, DynValue.Void, DynValue.NewNumber(2));
            DynValue nonFinalNull = script.Call(inspect, null, DynValue.NewNumber(2));

            await Assert.That(nonFinalTuple.Tuple[0].Boolean).IsFalse().ConfigureAwait(false);
            await Assert.That(nonFinalTuple.Tuple[1].Number).IsEqualTo(0d).ConfigureAwait(false);
            await Assert.That(nonFinalTuple.Tuple[2].Number).IsEqualTo(2d).ConfigureAwait(false);
            await Assert.That(nonFinalTuple.Tuple[3].Number).IsEqualTo(1d).ConfigureAwait(false);
            await Assert.That(nonFinalVoid.Tuple[0].Boolean).IsFalse().ConfigureAwait(false);
            await Assert.That(nonFinalVoid.Tuple[1].Number).IsEqualTo(0d).ConfigureAwait(false);
            await Assert.That(nonFinalVoid.Tuple[2].Number).IsEqualTo(2d).ConfigureAwait(false);
            await Assert
                .That(nonFinalVoid.Tuple[3].Type)
                .IsEqualTo(DataType.Nil)
                .ConfigureAwait(false);
            await Assert.That(nonFinalNull.Tuple[0].Boolean).IsFalse().ConfigureAwait(false);
            await Assert.That(nonFinalNull.Tuple[1].Number).IsEqualTo(0d).ConfigureAwait(false);
            await Assert.That(nonFinalNull.Tuple[2].Number).IsEqualTo(2d).ConfigureAwait(false);
            await Assert
                .That(nonFinalNull.Tuple[3].Type)
                .IsEqualTo(DataType.Nil)
                .ConfigureAwait(false);
        }

        [global::TUnit.Core.Test]
        public async Task FixedDynValueCallToLegacyClrFunctionPreservesTrailingExpansionEdges()
        {
            Script script = new(CoreModulePresets.Complete);
            DynValue inspect = DynValue.NewCallback((_, args) => SummarizeArguments(args));

            DynValue oneVoid = script.Call(inspect, DynValue.Void);
            DynValue oneTuple = script.Call(
                inspect,
                DynValue.NewTuple(DynValue.NewNumber(2), null)
            );
            DynValue twoTuple = script.Call(
                inspect,
                DynValue.NewNumber(1),
                DynValue.NewTuple(DynValue.NewNumber(2), null)
            );
            DynValue threeVoid = script.Call(
                inspect,
                DynValue.NewNumber(1),
                DynValue.NewNumber(2),
                DynValue.Void
            );
            DynValue fourVoid = script.Call(
                inspect,
                DynValue.NewNumber(1),
                DynValue.NewNumber(2),
                DynValue.NewNumber(3),
                DynValue.Void
            );
            DynValue oneEmptyTuple = script.Call(inspect, DynValue.EmptyTuple);
            DynValue twoEmptyTuple = script.Call(
                inspect,
                DynValue.NewNumber(1),
                DynValue.EmptyTuple
            );

            await AssertArgumentSummary(oneVoid, count: 0d, nilCount: 0d, sum: 0d)
                .ConfigureAwait(false);
            await AssertArgumentSummary(oneTuple, count: 2d, nilCount: 1d, sum: 2d)
                .ConfigureAwait(false);
            await AssertArgumentSummary(twoTuple, count: 3d, nilCount: 1d, sum: 3d)
                .ConfigureAwait(false);
            await AssertArgumentSummary(threeVoid, count: 2d, nilCount: 0d, sum: 3d)
                .ConfigureAwait(false);
            await AssertArgumentSummary(fourVoid, count: 3d, nilCount: 0d, sum: 6d)
                .ConfigureAwait(false);
            await AssertArgumentSummary(oneEmptyTuple, count: 0d, nilCount: 0d, sum: 0d)
                .ConfigureAwait(false);
            await AssertArgumentSummary(twoEmptyTuple, count: 1d, nilCount: 0d, sum: 1d)
                .ConfigureAwait(false);
        }

        [global::TUnit.Core.Test]
        [global::TUnit.Core.Arguments(LuaCompatibilityVersion.Lua51)]
        [global::TUnit.Core.Arguments(LuaCompatibilityVersion.Lua52)]
        [global::TUnit.Core.Arguments(LuaCompatibilityVersion.Lua53)]
        [global::TUnit.Core.Arguments(LuaCompatibilityVersion.Lua54)]
        [global::TUnit.Core.Arguments(LuaCompatibilityVersion.Lua55)]
        public async Task FixedDynValueCallToLegacyClrFunctionMetamethodIncludesSelf(
            LuaCompatibilityVersion version
        )
        {
            Script script = new(version, CoreModulePresets.Complete);
            Table callable = new(script);
            Table meta = new(script);

            meta.Set(
                "__call",
                DynValue.NewCallback(
                    (_, args) =>
                        DynValue.NewTuple(
                            DynValue.NewBoolean(args.Count == 2),
                            DynValue.NewBoolean(args.IsMethodCall),
                            DynValue.NewBoolean(ReferenceEquals(args[0].Table, callable)),
                            args[1]
                        )
                )
            );
            callable.MetaTable = meta;

            DynValue result = script.Call(DynValue.NewTable(callable), DynValue.NewNumber(42));

            await Assert.That(result.Type).IsEqualTo(DataType.Tuple).ConfigureAwait(false);
            await Assert.That(result.Tuple[0].Boolean).IsTrue().ConfigureAwait(false);
            await Assert.That(result.Tuple[1].Boolean).IsFalse().ConfigureAwait(false);
            await Assert.That(result.Tuple[2].Boolean).IsTrue().ConfigureAwait(false);
            await Assert.That(result.Tuple[3].Number).IsEqualTo(42d).ConfigureAwait(false);
        }

        [global::TUnit.Core.Test]
        [LuaVersionsUntil(LuaCompatibilityVersion.Lua53)]
        public async Task FixedDynValueCallRejectsChainedCallMetamethodsBeforeLua54(
            LuaCompatibilityVersion version
        )
        {
            Script script = new(version, CoreModulePresets.Complete);
            Table target = new(script);
            Table proxy = new(script);
            Table targetMeta = new(script);
            Table proxyMeta = new(script);

            targetMeta.Set("__call", DynValue.NewTable(proxy));
            proxyMeta.Set(
                "__call",
                DynValue.NewCallback((_, _) => DynValue.NewString("unexpected"))
            );
            target.MetaTable = targetMeta;
            proxy.MetaTable = proxyMeta;

            ArgumentException exception = Assert.Throws<ArgumentException>(() =>
                script.Call(DynValue.NewTable(target))
            );

            await Assert.That(exception.Message).Contains("__call").ConfigureAwait(false);
        }

        [global::TUnit.Core.Test]
        [global::TUnit.Core.Arguments(LuaCompatibilityVersion.Lua51, 1)]
        [global::TUnit.Core.Arguments(LuaCompatibilityVersion.Lua51, 2)]
        [global::TUnit.Core.Arguments(LuaCompatibilityVersion.Lua51, 3)]
        [global::TUnit.Core.Arguments(LuaCompatibilityVersion.Lua51, 4)]
        [global::TUnit.Core.Arguments(LuaCompatibilityVersion.Lua51, 5)]
        [global::TUnit.Core.Arguments(LuaCompatibilityVersion.Lua52, 1)]
        [global::TUnit.Core.Arguments(LuaCompatibilityVersion.Lua52, 2)]
        [global::TUnit.Core.Arguments(LuaCompatibilityVersion.Lua52, 3)]
        [global::TUnit.Core.Arguments(LuaCompatibilityVersion.Lua52, 4)]
        [global::TUnit.Core.Arguments(LuaCompatibilityVersion.Lua52, 5)]
        [global::TUnit.Core.Arguments(LuaCompatibilityVersion.Lua53, 1)]
        [global::TUnit.Core.Arguments(LuaCompatibilityVersion.Lua53, 2)]
        [global::TUnit.Core.Arguments(LuaCompatibilityVersion.Lua53, 3)]
        [global::TUnit.Core.Arguments(LuaCompatibilityVersion.Lua53, 4)]
        [global::TUnit.Core.Arguments(LuaCompatibilityVersion.Lua53, 5)]
        public async Task FixedDynValueCallRejectsChainedCallMetamethodsBeforeLua54WithArguments(
            LuaCompatibilityVersion version,
            int arity
        )
        {
            Script script = new(version, CoreModulePresets.Complete);
            DynValue target = CreateTableValuedCallChain(
                script,
                (_, _, _) => DynValue.NewString("unexpected")
            );

            ArgumentException exception = Assert.Throws<ArgumentException>(() =>
                CallLegacyCallbackWithSequentialArguments(script, target, arity)
            );

            await Assert.That(exception.Message).Contains("__call").ConfigureAwait(false);
        }

        [global::TUnit.Core.Test]
        [LuaVersionsFrom(LuaCompatibilityVersion.Lua54)]
        public async Task FixedDynValueCallFollowsChainedCallMetamethodsWithSelfArguments(
            LuaCompatibilityVersion version
        )
        {
            Script script = new(version, CoreModulePresets.Complete);
            Table target = new(script);
            Table proxy = new(script);
            Table targetMeta = new(script);
            Table proxyMeta = new(script);

            targetMeta.Set("__call", DynValue.NewTable(proxy));
            proxyMeta.Set(
                "__call",
                DynValue.NewCallback(
                    (_, args) =>
                        DynValue.NewTuple(
                            DynValue.NewBoolean(args.Count == 2),
                            DynValue.NewBoolean(ReferenceEquals(args[0].Table, proxy)),
                            DynValue.NewBoolean(ReferenceEquals(args[1].Table, target)),
                            DynValue.NewBoolean(args.IsMethodCall)
                        )
                )
            );
            target.MetaTable = targetMeta;
            proxy.MetaTable = proxyMeta;

            DynValue result = script.Call(DynValue.NewTable(target));

            await Assert.That(result.Type).IsEqualTo(DataType.Tuple).ConfigureAwait(false);
            await Assert.That(result.Tuple[0].Boolean).IsTrue().ConfigureAwait(false);
            await Assert.That(result.Tuple[1].Boolean).IsTrue().ConfigureAwait(false);
            await Assert.That(result.Tuple[2].Boolean).IsTrue().ConfigureAwait(false);
            await Assert.That(result.Tuple[3].Boolean).IsFalse().ConfigureAwait(false);
        }

        [global::TUnit.Core.Test]
        [global::TUnit.Core.Arguments(LuaCompatibilityVersion.Lua54, 1)]
        [global::TUnit.Core.Arguments(LuaCompatibilityVersion.Lua54, 2)]
        [global::TUnit.Core.Arguments(LuaCompatibilityVersion.Lua54, 3)]
        [global::TUnit.Core.Arguments(LuaCompatibilityVersion.Lua54, 4)]
        [global::TUnit.Core.Arguments(LuaCompatibilityVersion.Lua54, 5)]
        [global::TUnit.Core.Arguments(LuaCompatibilityVersion.Lua55, 1)]
        [global::TUnit.Core.Arguments(LuaCompatibilityVersion.Lua55, 2)]
        [global::TUnit.Core.Arguments(LuaCompatibilityVersion.Lua55, 3)]
        [global::TUnit.Core.Arguments(LuaCompatibilityVersion.Lua55, 4)]
        [global::TUnit.Core.Arguments(LuaCompatibilityVersion.Lua55, 5)]
        public async Task FixedDynValueCallFollowsChainedCallMetamethodsWithArguments(
            LuaCompatibilityVersion version,
            int arity
        )
        {
            Script script = new(version, CoreModulePresets.Complete);
            DynValue target = CreateTableValuedCallChain(
                script,
                (targetTable, proxyTable, args) =>
                    SummarizeChainedCallArguments(targetTable, proxyTable, args)
            );

            DynValue result = CallLegacyCallbackWithSequentialArguments(script, target, arity);

            await AssertChainedCallSummary(result, arity).ConfigureAwait(false);
        }

        [global::TUnit.Core.Test]
        [global::TUnit.Core.Arguments(1)]
        [global::TUnit.Core.Arguments(2)]
        [global::TUnit.Core.Arguments(3)]
        [global::TUnit.Core.Arguments(4)]
        [global::TUnit.Core.Arguments(5)]
        public async Task DefaultFixedDynValueCallFollowsChainedCallMetamethodsWithArguments(
            int arity
        )
        {
            Script script = new(CoreModulePresets.Complete);
            DynValue target = CreateTableValuedCallChain(
                script,
                (targetTable, proxyTable, args) =>
                    SummarizeChainedCallArguments(targetTable, proxyTable, args)
            );

            DynValue result = CallLegacyCallbackWithSequentialArguments(script, target, arity);

            await AssertChainedCallSummary(result, arity).ConfigureAwait(false);
        }

        [global::TUnit.Core.Test]
        [LuaVersionsFrom(LuaCompatibilityVersion.Lua54)]
        public async Task FixedDynValueCallThrowsLoopInCallWhenCallMetamethodChainExceedsLimit(
            LuaCompatibilityVersion version
        )
        {
            Script script = new(version, CoreModulePresets.Complete);
            Table root = new(script);
            Table current = root;

            for (int i = 0; i < 15; i++)
            {
                Table next = new(script);
                Table meta = new(script);
                meta.Set("__call", DynValue.NewTable(next));
                current.MetaTable = meta;
                current = next;
            }

            ScriptRuntimeException exception = Assert.Throws<ScriptRuntimeException>(() =>
                script.Call(DynValue.NewTable(root))
            );

            await Assert.That(exception.Message).Contains("loop").ConfigureAwait(false);
        }

        [global::TUnit.Core.Test]
        public async Task FixedDynValueCallToLegacyClrFunctionAvoidsArgumentArrayAllocation()
        {
            const int iterations = 1024;
            Script script = new(CoreModulePresets.Complete);
            DynValue first = DynValue.NewNumber(10);
            DynValue second = DynValue.NewNumber(20);
            DynValue third = DynValue.NewNumber(30);
            DynValue noArgCallback = DynValue.NewCallback(
                (_, args) =>
                {
                    if (args.Count != 0)
                    {
                        throw new InvalidOperationException("Unexpected no-arg callback arity.");
                    }

                    return DynValue.Nil;
                }
            );
            DynValue fixedCallback = DynValue.NewCallback(
                (_, args) =>
                {
                    if (args.Count != 3)
                    {
                        throw new InvalidOperationException("Unexpected fixed callback arity.");
                    }

                    return args[2];
                }
            );
            DynValue spanProbeCallback = DynValue.NewCallback(
                (_, args) =>
                {
                    bool spanAvailable = args.TryGetSpan(out ReadOnlySpan<DynValue> span);
                    if (!spanAvailable || span.Length != 3)
                    {
                        throw new InvalidOperationException("Unexpected span probe state.");
                    }

                    if (args.Count != 3)
                    {
                        throw new InvalidOperationException(
                            "Unexpected span-probe callback arity."
                        );
                    }

                    return args[2];
                }
            );

            MeasureNoArgumentLegacyCallbackAllocations(script, noArgCallback, iterations: 8);
            MeasureFixedThreeArgumentLegacyCallbackAllocations(
                script,
                fixedCallback,
                first,
                second,
                third,
                iterations: 8
            );
            MeasureFixedThreeArgumentLegacyCallbackAllocations(
                script,
                spanProbeCallback,
                first,
                second,
                third,
                iterations: 8
            );

            GC.Collect();
            GC.WaitForPendingFinalizers();
            GC.Collect();

            long noArgumentAllocated = MeasureNoArgumentLegacyCallbackAllocations(
                script,
                noArgCallback,
                iterations
            );

            GC.Collect();
            GC.WaitForPendingFinalizers();
            GC.Collect();

            long fixedArgumentAllocated = MeasureFixedThreeArgumentLegacyCallbackAllocations(
                script,
                fixedCallback,
                first,
                second,
                third,
                iterations
            );

            GC.Collect();
            GC.WaitForPendingFinalizers();
            GC.Collect();

            long spanProbeAllocated = MeasureFixedThreeArgumentLegacyCallbackAllocations(
                script,
                spanProbeCallback,
                first,
                second,
                third,
                iterations
            );

            long extraBytesPerCall = (fixedArgumentAllocated - noArgumentAllocated) / iterations;
            long spanProbeExtraBytesPerCall =
                (spanProbeAllocated - noArgumentAllocated) / iterations;

            await Assert.That(extraBytesPerCall).IsLessThan(16).ConfigureAwait(false);
            await Assert.That(spanProbeExtraBytesPerCall).IsLessThan(16).ConfigureAwait(false);
        }

        [global::TUnit.Core.Test]
        public async Task FixedDynValueCallToCallbackViewMetamethodAvoidsArgumentArrayAllocation()
        {
            const int iterations = 1024;
            Script script = new(CoreModulePresets.Complete);
            Table callable = new(script);
            Table meta = new(script);
            DynValue callableValue = DynValue.NewTable(callable);
            DynValue first = DynValue.NewNumber(1);
            DynValue second = DynValue.NewNumber(2);
            DynValue third = DynValue.NewNumber(3);
            DynValue fourth = DynValue.NewNumber(4);
            DynValue callback = DynValue.NewCallbackView(
                (_, args) =>
                {
                    if (
                        args.Count != 5
                        || !ReferenceEquals(args[0].Table, callable)
                        || args[1].Number != 1d
                        || args[2].Number != 2d
                        || args[3].Number != 3d
                        || args[4].Number != 4d
                    )
                    {
                        throw new InvalidOperationException(
                            "Metamethod allocation probe received unexpected arguments."
                        );
                    }

                    return DynValue.Nil;
                }
            );
            meta.Set("__call", callback);
            callable.MetaTable = meta;

            MeasureDirectFiveArgumentCallbackViewAllocations(
                script,
                callback,
                callableValue,
                first,
                second,
                third,
                fourth,
                iterations: 8
            );
            MeasureFixedFourArgumentCallbackViewMetamethodAllocations(
                script,
                callableValue,
                first,
                second,
                third,
                fourth,
                iterations: 8
            );

            GC.Collect();
            GC.WaitForPendingFinalizers();
            GC.Collect();

            long directAllocated = MeasureDirectFiveArgumentCallbackViewAllocations(
                script,
                callback,
                callableValue,
                first,
                second,
                third,
                fourth,
                iterations
            );

            GC.Collect();
            GC.WaitForPendingFinalizers();
            GC.Collect();

            long metamethodAllocated = MeasureFixedFourArgumentCallbackViewMetamethodAllocations(
                script,
                callableValue,
                first,
                second,
                third,
                fourth,
                iterations
            );
            long extraBytesPerCall = (metamethodAllocated - directAllocated) / iterations;

            await Assert.That(extraBytesPerCall).IsLessThan(16).ConfigureAwait(false);
        }

        [global::TUnit.Core.Test]
        public async Task FixedFiveDynValueCallToCallbackViewMetamethodAvoidsArgumentArrayAllocation()
        {
            const int iterations = 1024;
            Script script = new(CoreModulePresets.Complete);
            Table callable = new(script);
            Table meta = new(script);
            DynValue callableValue = DynValue.NewTable(callable);
            DynValue first = DynValue.NewNumber(1);
            DynValue second = DynValue.NewNumber(2);
            DynValue third = DynValue.NewNumber(3);
            DynValue fourth = DynValue.NewNumber(4);
            DynValue fifth = DynValue.NewNumber(5);
            DynValue fourArgumentCallback = DynValue.NewCallbackView(
                (_, args) =>
                {
                    if (
                        args.Count != 5
                        || !ReferenceEquals(args[0].Table, callable)
                        || args[1].Number != 1d
                        || args[2].Number != 2d
                        || args[3].Number != 3d
                        || args[4].Number != 4d
                    )
                    {
                        throw new InvalidOperationException(
                            "Four-argument metamethod allocation probe received unexpected arguments."
                        );
                    }

                    return DynValue.Nil;
                }
            );
            DynValue fiveArgumentCallback = DynValue.NewCallbackView(
                (_, args) =>
                {
                    if (
                        args.Count != 6
                        || !ReferenceEquals(args[0].Table, callable)
                        || args[1].Number != 1d
                        || args[2].Number != 2d
                        || args[3].Number != 3d
                        || args[4].Number != 4d
                        || args[5].Number != 5d
                    )
                    {
                        throw new InvalidOperationException(
                            "Five-argument metamethod allocation probe received unexpected arguments."
                        );
                    }

                    return DynValue.Nil;
                }
            );
            callable.MetaTable = meta;

            meta.Set("__call", fourArgumentCallback);
            MeasureFixedFourArgumentCallbackViewMetamethodAllocations(
                script,
                callableValue,
                first,
                second,
                third,
                fourth,
                iterations: 8
            );
            meta.Set("__call", fiveArgumentCallback);
            MeasureFixedFiveArgumentCallbackViewMetamethodAllocations(
                script,
                callableValue,
                first,
                second,
                third,
                fourth,
                fifth,
                iterations: 8
            );

            GC.Collect();
            GC.WaitForPendingFinalizers();
            GC.Collect();

            meta.Set("__call", fourArgumentCallback);
            long fourArgumentAllocated = MeasureFixedFourArgumentCallbackViewMetamethodAllocations(
                script,
                callableValue,
                first,
                second,
                third,
                fourth,
                iterations
            );

            GC.Collect();
            GC.WaitForPendingFinalizers();
            GC.Collect();

            meta.Set("__call", fiveArgumentCallback);
            long metamethodAllocated = MeasureFixedFiveArgumentCallbackViewMetamethodAllocations(
                script,
                callableValue,
                first,
                second,
                third,
                fourth,
                fifth,
                iterations
            );
            long extraBytesPerCall = (metamethodAllocated - fourArgumentAllocated) / iterations;

            await Assert.That(extraBytesPerCall).IsLessThan(16).ConfigureAwait(false);
        }

        [global::TUnit.Core.Test]
        public async Task FixedDynValueCallToChainedCallbackViewMetamethodAvoidsArgumentArrayAllocation()
        {
            const int iterations = 1024;
            Script script = new(CoreModulePresets.Complete);
            Table target = new(script);
            Table proxy = new(script);
            Table targetMeta = new(script);
            Table proxyMeta = new(script);
            DynValue targetValue = DynValue.NewTable(target);
            DynValue proxyValue = DynValue.NewTable(proxy);
            DynValue first = DynValue.NewNumber(1);
            DynValue second = DynValue.NewNumber(2);
            DynValue third = DynValue.NewNumber(3);
            DynValue callback = DynValue.NewCallbackView(
                (_, args) =>
                {
                    if (
                        args.Count != 5
                        || !ReferenceEquals(args[0].Table, proxy)
                        || !ReferenceEquals(args[1].Table, target)
                        || args[2].Number != 1d
                        || args[3].Number != 2d
                        || args[4].Number != 3d
                    )
                    {
                        throw new InvalidOperationException(
                            "Chained metamethod allocation probe received unexpected arguments."
                        );
                    }

                    return DynValue.Nil;
                }
            );
            targetMeta.Set("__call", proxyValue);
            proxyMeta.Set("__call", callback);
            target.MetaTable = targetMeta;
            proxy.MetaTable = proxyMeta;

            MeasureDirectFiveArgumentCallbackViewAllocations(
                script,
                callback,
                proxyValue,
                targetValue,
                first,
                second,
                third,
                iterations: 8
            );
            MeasureFixedThreeArgumentCallbackViewChainedMetamethodAllocations(
                script,
                targetValue,
                first,
                second,
                third,
                iterations: 8
            );

            GC.Collect();
            GC.WaitForPendingFinalizers();
            GC.Collect();

            long directAllocated = MeasureDirectFiveArgumentCallbackViewAllocations(
                script,
                callback,
                proxyValue,
                targetValue,
                first,
                second,
                third,
                iterations
            );

            GC.Collect();
            GC.WaitForPendingFinalizers();
            GC.Collect();

            long metamethodAllocated =
                MeasureFixedThreeArgumentCallbackViewChainedMetamethodAllocations(
                    script,
                    targetValue,
                    first,
                    second,
                    third,
                    iterations
                );
            long extraBytesPerCall = (metamethodAllocated - directAllocated) / iterations;

            await Assert.That(extraBytesPerCall).IsLessThan(16).ConfigureAwait(false);
        }

        [global::TUnit.Core.Test]
        public async Task FixedFiveDynValueCallToChainedCallbackViewMetamethodAvoidsArgumentArrayAllocation()
        {
            const int iterations = 1024;
            Script script = new(CoreModulePresets.Complete);
            Table target = new(script);
            Table proxy = new(script);
            Table targetMeta = new(script);
            Table proxyMeta = new(script);
            DynValue targetValue = DynValue.NewTable(target);
            DynValue proxyValue = DynValue.NewTable(proxy);
            DynValue first = DynValue.NewNumber(1);
            DynValue second = DynValue.NewNumber(2);
            DynValue third = DynValue.NewNumber(3);
            DynValue fourth = DynValue.NewNumber(4);
            DynValue fifth = DynValue.NewNumber(5);
            DynValue callback = DynValue.NewCallbackView(
                (_, args) =>
                {
                    if (
                        (args.Count != 6 && args.Count != 7)
                        || !ReferenceEquals(args[0].Table, proxy)
                        || !ReferenceEquals(args[1].Table, target)
                    )
                    {
                        throw new InvalidOperationException(
                            "Five-argument chained metamethod allocation probe received unexpected self arguments."
                        );
                    }

                    for (int i = 2; i < args.Count; i++)
                    {
                        if (args[i].Number != i - 1d)
                        {
                            throw new InvalidOperationException(
                                "Five-argument chained metamethod allocation probe received unexpected user arguments."
                            );
                        }
                    }

                    return DynValue.Nil;
                }
            );
            targetMeta.Set("__call", proxyValue);
            proxyMeta.Set("__call", callback);
            target.MetaTable = targetMeta;
            proxy.MetaTable = proxyMeta;

            MeasureFixedFourArgumentCallbackViewChainedMetamethodAllocations(
                script,
                targetValue,
                first,
                second,
                third,
                fourth,
                iterations: 8
            );
            MeasureFixedFiveArgumentCallbackViewChainedMetamethodAllocations(
                script,
                targetValue,
                first,
                second,
                third,
                fourth,
                fifth,
                iterations: 8
            );

            GC.Collect();
            GC.WaitForPendingFinalizers();
            GC.Collect();

            long fourArgumentAllocated =
                MeasureFixedFourArgumentCallbackViewChainedMetamethodAllocations(
                    script,
                    targetValue,
                    first,
                    second,
                    third,
                    fourth,
                    iterations
                );

            GC.Collect();
            GC.WaitForPendingFinalizers();
            GC.Collect();

            long fiveArgumentAllocated =
                MeasureFixedFiveArgumentCallbackViewChainedMetamethodAllocations(
                    script,
                    targetValue,
                    first,
                    second,
                    third,
                    fourth,
                    fifth,
                    iterations
                );
            long extraBytesPerCall = (fiveArgumentAllocated - fourArgumentAllocated) / iterations;

            await Assert
                .That(extraBytesPerCall)
                .IsLessThan(16)
                .Because(
                    $"Four-user-argument chained calls allocated {fourArgumentAllocated} bytes; five-user-argument chained calls allocated {fiveArgumentAllocated} bytes."
                )
                .ConfigureAwait(false);
        }

        [global::TUnit.Core.Test]
        public async Task SpanAndArrayDynValueCallToCallbackViewMetamethodAvoidArgumentArrayAllocation()
        {
            const int iterations = 1024;
            Script script = new(CoreModulePresets.Complete);
            Table callable = new(script);
            Table meta = new(script);
            DynValue callableValue = DynValue.NewTable(callable);
            DynValue[] args =
            {
                DynValue.NewNumber(1),
                DynValue.NewNumber(2),
                DynValue.NewNumber(3),
                DynValue.NewNumber(4),
            };
            DynValue callback = DynValue.NewCallbackView(
                (_, callbackArgs) =>
                {
                    if (
                        callbackArgs.Count != 5
                        || !ReferenceEquals(callbackArgs[0].Table, callable)
                        || callbackArgs[1].Number != 1d
                        || callbackArgs[2].Number != 2d
                        || callbackArgs[3].Number != 3d
                        || callbackArgs[4].Number != 4d
                    )
                    {
                        throw new InvalidOperationException(
                            "Span/array metamethod allocation probe received unexpected arguments."
                        );
                    }

                    return DynValue.Nil;
                }
            );
            meta.Set("__call", callback);
            callable.MetaTable = meta;

            MeasureDirectFiveArgumentCallbackViewAllocations(
                script,
                callback,
                callableValue,
                args[0],
                args[1],
                args[2],
                args[3],
                iterations: 8
            );
            MeasureSpanCallbackViewMetamethodAllocations(
                script,
                callableValue,
                args,
                iterations: 8
            );
            MeasureArrayCallbackViewMetamethodAllocations(
                script,
                callableValue,
                args,
                iterations: 8
            );

            GC.Collect();
            GC.WaitForPendingFinalizers();
            GC.Collect();

            long directAllocated = MeasureDirectFiveArgumentCallbackViewAllocations(
                script,
                callback,
                callableValue,
                args[0],
                args[1],
                args[2],
                args[3],
                iterations
            );

            GC.Collect();
            GC.WaitForPendingFinalizers();
            GC.Collect();

            long spanAllocated = MeasureSpanCallbackViewMetamethodAllocations(
                script,
                callableValue,
                args,
                iterations
            );

            GC.Collect();
            GC.WaitForPendingFinalizers();
            GC.Collect();

            long arrayAllocated = MeasureArrayCallbackViewMetamethodAllocations(
                script,
                callableValue,
                args,
                iterations
            );
            long spanExtraBytesPerCall = (spanAllocated - directAllocated) / iterations;
            long arrayExtraBytesPerCall = (arrayAllocated - directAllocated) / iterations;

            await Assert.That(spanExtraBytesPerCall).IsLessThan(16).ConfigureAwait(false);
            await Assert.That(arrayExtraBytesPerCall).IsLessThan(16).ConfigureAwait(false);
        }

        [global::TUnit.Core.Test]
        public async Task SpanAndArrayDynValueCallToMetamethodPreserveSpecialArgumentAdjustment()
        {
            Script script = new(CoreModulePresets.Complete);
            Table callable = new(script);
            Table meta = new(script);
            DynValue callableValue = DynValue.NewTable(callable);
            DynValue inspect = DynValue.NewCallback((_, args) => SummarizeArguments(args));
            meta.Set("__call", inspect);
            callable.MetaTable = meta;
            DynValue[] spanArgs =
            {
                null,
                DynValue.NewTuple(DynValue.NewNumber(2), DynValue.NewNumber(20)),
                DynValue.NewNumber(3),
                DynValue.NewTuple(DynValue.NewNumber(4), null),
            };
            DynValue[] arrayArgs =
            {
                DynValue.NewNumber(1),
                null,
                DynValue.NewTuple(DynValue.NewNumber(2), DynValue.NewNumber(20)),
                DynValue.NewNumber(3),
                DynValue.Void,
            };

            DynValue spanResult = script.Call(callableValue, spanArgs.AsSpan());
            DynValue arrayResult = script.Call(callableValue, arrayArgs);

            await AssertArgumentSummary(spanResult, count: 6d, nilCount: 2d, sum: 9d)
                .ConfigureAwait(false);
            await AssertArgumentSummary(arrayResult, count: 5d, nilCount: 1d, sum: 6d)
                .ConfigureAwait(false);
        }

        [global::TUnit.Core.Test]
        public async Task FixedFiveDynValueCallToMetamethodPreservesSpecialArgumentAdjustment()
        {
            Script script = new(CoreModulePresets.Complete);
            Table callable = new(script);
            Table meta = new(script);
            DynValue callableValue = DynValue.NewTable(callable);
            DynValue[] values =
            {
                DynValue.NewNumber(1),
                null,
                DynValue.NewTuple(DynValue.NewNumber(2), DynValue.NewNumber(20)),
                DynValue.NewNumber(3),
                DynValue.NewTuple(DynValue.NewNumber(4), null),
            };

            meta.Set(
                "__call",
                DynValue.NewCallback((_, args) => SummarizeArgumentsSkippingFirst(args))
            );
            callable.MetaTable = meta;
            DynValue legacyResult = script.Call(
                callableValue,
                values[0],
                values[1],
                values[2],
                values[3],
                values[4]
            );

            meta.Set(
                "__call",
                DynValue.NewCallbackView((_, args) => SummarizeArgumentsSkippingFirst(args))
            );
            DynValue viewResult = script.Call(
                callableValue,
                values[0],
                values[1],
                values[2],
                values[3],
                values[4]
            );

            await AssertArgumentSummary(legacyResult, count: 6d, nilCount: 2d, sum: 10d)
                .ConfigureAwait(false);
            await AssertArgumentSummary(viewResult, count: 6d, nilCount: 2d, sum: 10d)
                .ConfigureAwait(false);
        }

        [global::TUnit.Core.Test]
        [global::TUnit.Core.Arguments(LuaCompatibilityVersion.Lua51)]
        [global::TUnit.Core.Arguments(LuaCompatibilityVersion.Lua52)]
        [global::TUnit.Core.Arguments(LuaCompatibilityVersion.Lua53)]
        [global::TUnit.Core.Arguments(LuaCompatibilityVersion.Lua54)]
        [global::TUnit.Core.Arguments(LuaCompatibilityVersion.Lua55)]
        public async Task FixedDynValueCallToCallbackViewMetamethodIncludesSelf(
            LuaCompatibilityVersion version
        )
        {
            Script script = new(version, CoreModulePresets.Complete);
            Table callable = new(script);
            Table meta = new(script);
            bool sawSelf = false;

            meta.Set(
                "__call",
                DynValue.NewCallbackView(
                    (_, args) =>
                    {
                        sawSelf = args.Count == 2 && ReferenceEquals(args[0].Table, callable);
                        return DynValue.NewNumber(args[1].Number + args.Count);
                    }
                )
            );
            callable.MetaTable = meta;

            DynValue result = script.Call(DynValue.NewTable(callable), DynValue.NewNumber(40));

            await Assert.That(result.Number).IsEqualTo(42d).ConfigureAwait(false);
            await Assert.That(sawSelf).IsTrue().ConfigureAwait(false);
        }

        [global::TUnit.Core.Test]
        [global::TUnit.Core.Arguments(LuaCompatibilityVersion.Lua51)]
        [global::TUnit.Core.Arguments(LuaCompatibilityVersion.Lua52)]
        [global::TUnit.Core.Arguments(LuaCompatibilityVersion.Lua53)]
        [global::TUnit.Core.Arguments(LuaCompatibilityVersion.Lua54)]
        [global::TUnit.Core.Arguments(LuaCompatibilityVersion.Lua55)]
        public async Task FourDynValueCallToCallbackViewMetamethodPreservesArguments(
            LuaCompatibilityVersion version
        )
        {
            Script script = new(version, CoreModulePresets.Complete);
            Table callable = new(script);
            Table meta = new(script);
            bool sawSelf = false;
            double sum = 0d;

            meta.Set(
                "__call",
                DynValue.NewCallbackView(
                    (_, args) =>
                    {
                        sawSelf = args.Count == 5 && ReferenceEquals(args[0].Table, callable);
                        for (int i = 1; i < args.Count; i++)
                        {
                            sum += args[i].Number;
                        }

                        return DynValue.NewNumber(args.Count);
                    }
                )
            );
            callable.MetaTable = meta;

            DynValue result = script.Call(
                DynValue.NewTable(callable),
                DynValue.NewNumber(1),
                DynValue.NewNumber(2),
                DynValue.NewNumber(3),
                DynValue.NewNumber(4)
            );

            await Assert.That(result.Number).IsEqualTo(5d).ConfigureAwait(false);
            await Assert.That(sawSelf).IsTrue().ConfigureAwait(false);
            await Assert.That(sum).IsEqualTo(10d).ConfigureAwait(false);
        }

        [global::TUnit.Core.Test]
        [AllLuaVersions]
        public async Task CallWithReadOnlySpanDynValuesInvokesLuaFunction(
            LuaCompatibilityVersion version
        )
        {
            Script script = new(version, CoreModulePresets.Complete);
            DynValue function = script.DoString(
                "return function(a, b, c, d, e) return a + b + c + d + e end"
            );
            DynValue[] args = CreateSequentialArguments(5);

            DynValue result = script.Call(function, args.AsSpan());

            await Assert.That(result.Number).IsEqualTo(15d).ConfigureAwait(false);
        }

        [global::TUnit.Core.Test]
        [AllLuaVersions]
        public async Task CallWithReadOnlySpanDynValuesPreservesAdjustmentSemantics(
            LuaCompatibilityVersion version
        )
        {
            Script script = new(version, CoreModulePresets.Complete);
            DynValue inspect = script.DoString(
                @"
                return function(...)
                    local count = select('#', ...)
                    local nils = 0
                    local sum = 0
                    for i = 1, count do
                        local value = select(i, ...)
                        if value == nil then
                            nils = nils + 1
                        else
                            sum = sum + value
                        end
                    end

                    return count, nils, sum
                end
                "
            );
            DynValue[] args =
            {
                DynValue.NewNumber(1),
                null,
                DynValue.NewTuple(DynValue.NewNumber(2), DynValue.NewNumber(20)),
                DynValue.NewNumber(3),
                DynValue.NewTuple(DynValue.NewNumber(4), null),
            };

            DynValue result = script.Call(inspect, args.AsSpan());

            await AssertArgumentSummary(result, count: 6d, nilCount: 2d, sum: 10d)
                .ConfigureAwait(false);
        }

        [global::TUnit.Core.Test]
        [global::TUnit.Core.Arguments(0)]
        [global::TUnit.Core.Arguments(1)]
        [global::TUnit.Core.Arguments(2)]
        [global::TUnit.Core.Arguments(3)]
        [global::TUnit.Core.Arguments(4)]
        [global::TUnit.Core.Arguments(5)]
        public async Task CallWithReadOnlySpanDynValuesExposesSpanToCallbackView(int arity)
        {
            Script script = new(CoreModulePresets.Complete);
            DynValue callback = DynValue.NewCallbackView(
                (_, args) =>
                {
                    bool spanAvailable = args.TryGetSpan(out ReadOnlySpan<DynValue> span);
                    double sum = 0d;
                    for (int i = 0; i < span.Length; i++)
                    {
                        sum += span[i].Number;
                    }

                    return DynValue.NewTuple(
                        DynValue.NewBoolean(spanAvailable),
                        DynValue.NewNumber(span.Length),
                        DynValue.NewNumber(args.Count),
                        DynValue.NewNumber(sum)
                    );
                }
            );
            DynValue[] values = CreateSequentialArguments(arity);

            DynValue result = script.Call(callback, values.AsSpan());

            await Assert.That(result.Type).IsEqualTo(DataType.Tuple).ConfigureAwait(false);
            await Assert.That(result.Tuple[0].Boolean).IsTrue().ConfigureAwait(false);
            await Assert
                .That(result.Tuple[1].Number)
                .IsEqualTo((double)arity)
                .ConfigureAwait(false);
            await Assert
                .That(result.Tuple[2].Number)
                .IsEqualTo((double)arity)
                .ConfigureAwait(false);
            await Assert
                .That(result.Tuple[3].Number)
                .IsEqualTo(arity * (arity + 1) / 2d)
                .ConfigureAwait(false);
        }

        [global::TUnit.Core.Test]
        [AllLuaVersions]
        public async Task CallWithReadOnlySpanDynValuesDoesNotExposeSpanWhenNormalizationIsNeeded(
            LuaCompatibilityVersion version
        )
        {
            Script script = new(version, CoreModulePresets.Complete);
            DynValue callback = DynValue.NewCallbackView(
                (_, args) =>
                {
                    bool spanAvailable = args.TryGetSpan(out ReadOnlySpan<DynValue> span);
                    return DynValue.NewTuple(
                        DynValue.NewBoolean(spanAvailable),
                        DynValue.NewNumber(span.Length),
                        DynValue.NewNumber(args.Count),
                        args[0],
                        args[5]
                    );
                }
            );
            DynValue[] values =
            {
                null,
                DynValue.NewNumber(2),
                DynValue.NewTuple(DynValue.NewNumber(3), DynValue.NewNumber(30)),
                DynValue.NewNumber(4),
                DynValue.NewTuple(DynValue.NewNumber(5), null),
            };

            DynValue result = script.Call(callback, values.AsSpan());

            await Assert.That(result.Type).IsEqualTo(DataType.Tuple).ConfigureAwait(false);
            await Assert.That(result.Tuple[0].Boolean).IsFalse().ConfigureAwait(false);
            await Assert.That(result.Tuple[1].Number).IsEqualTo(0d).ConfigureAwait(false);
            await Assert.That(result.Tuple[2].Number).IsEqualTo(6d).ConfigureAwait(false);
            await Assert.That(result.Tuple[3].Type).IsEqualTo(DataType.Nil).ConfigureAwait(false);
            await Assert.That(result.Tuple[4].Type).IsEqualTo(DataType.Nil).ConfigureAwait(false);
        }

        [global::TUnit.Core.Test]
        [AllLuaVersions]
        public async Task CallWithReadOnlySpanDynValuesPreservesLegacyCallbackSpecialArguments(
            LuaCompatibilityVersion version
        )
        {
            Script script = new(version, CoreModulePresets.Complete);
            DynValue inspect = DynValue.NewCallback((_, args) => SummarizeArguments(args));
            DynValue[] values =
            {
                DynValue.NewNumber(1),
                null,
                DynValue.NewTuple(DynValue.NewNumber(2), DynValue.NewNumber(20)),
                DynValue.NewNumber(3),
                DynValue.NewTuple(DynValue.NewNumber(4), null),
            };

            DynValue result = script.Call(inspect, values.AsSpan());

            await AssertArgumentSummary(result, count: 6d, nilCount: 2d, sum: 10d)
                .ConfigureAwait(false);
        }

        [global::TUnit.Core.Test]
        [AllLuaVersions]
        public async Task CallWithReadOnlySpanDynValuesIncludesSelfForCallMetamethod(
            LuaCompatibilityVersion version
        )
        {
            Script script = new(version, CoreModulePresets.Complete);
            script.DoString(
                @"
                local mt = {}
                function mt:__call(a, b, c, d, e)
                    return self.marker + a + b + c + d + e
                end

                callable = setmetatable({ marker = 100 }, mt)
                "
            );
            DynValue[] args = CreateSequentialArguments(5);

            DynValue result = script.Call(script.Globals.Get("callable"), args.AsSpan());

            await Assert.That(result.Number).IsEqualTo(115d).ConfigureAwait(false);
        }

        [global::TUnit.Core.Test]
        [LuaVersionsUntil(LuaCompatibilityVersion.Lua53)]
        public async Task CallWithReadOnlySpanDynValuesRejectsChainedCallMetamethodsBeforeLua54(
            LuaCompatibilityVersion version
        )
        {
            Script script = new(version, CoreModulePresets.Complete);
            DynValue target = CreateTableValuedCallChain(
                script,
                (_, _, _) => DynValue.NewString("unexpected")
            );
            DynValue[] args = CreateSequentialArguments(5);

            ArgumentException exception = Assert.Throws<ArgumentException>(() =>
                script.Call(target, args.AsSpan())
            );

            await Assert.That(exception.Message).Contains("__call").ConfigureAwait(false);
        }

        [global::TUnit.Core.Test]
        [LuaVersionsFrom(LuaCompatibilityVersion.Lua54)]
        public async Task CallWithReadOnlySpanDynValuesFollowsChainedCallMetamethodsFromLua54(
            LuaCompatibilityVersion version
        )
        {
            Script script = new(version, CoreModulePresets.Complete);
            DynValue target = CreateTableValuedCallChain(
                script,
                (targetTable, proxyTable, args) =>
                    SummarizeChainedCallArguments(targetTable, proxyTable, args)
            );
            DynValue[] args = CreateSequentialArguments(5);

            DynValue result = script.Call(target, args.AsSpan());

            await AssertChainedCallSummary(result, userArity: 5).ConfigureAwait(false);
        }

        [global::TUnit.Core.Test]
        [global::TUnit.Core.Arguments(LuaCompatibilityVersion.Lua51)]
        [global::TUnit.Core.Arguments(LuaCompatibilityVersion.Lua52)]
        [global::TUnit.Core.Arguments(LuaCompatibilityVersion.Lua53)]
        [global::TUnit.Core.Arguments(LuaCompatibilityVersion.Lua54)]
        [global::TUnit.Core.Arguments(LuaCompatibilityVersion.Lua55)]
        public async Task CallRejectsNonCallableValues(LuaCompatibilityVersion version)
        {
            Script script = new(version, CoreModulePresets.Complete);
            DynValue notCallable = DynValue.NewString("nope");

            ArgumentException exception = Assert.Throws<ArgumentException>(() =>
                script.Call(notCallable)
            );

            await Assert
                .That(exception.Message)
                .Contains("has no __call metamethod")
                .ConfigureAwait(false);
        }

        [global::TUnit.Core.Test]
        [global::TUnit.Core.Arguments(LuaCompatibilityVersion.Lua51)]
        [global::TUnit.Core.Arguments(LuaCompatibilityVersion.Lua52)]
        [global::TUnit.Core.Arguments(LuaCompatibilityVersion.Lua53)]
        [global::TUnit.Core.Arguments(LuaCompatibilityVersion.Lua54)]
        [global::TUnit.Core.Arguments(LuaCompatibilityVersion.Lua55)]
        public async Task CallWithObjectArgumentsConvertsValues(LuaCompatibilityVersion version)
        {
            Script script = new(version, CoreModulePresets.Complete);
            script.DoString("function add(a, b) return a + b end");
            DynValue function = script.Globals.Get("add");

            DynValue result = script.Call(function, 30, 12);

            await Assert.That(result.Number).IsEqualTo(42d).ConfigureAwait(false);
        }

        [global::TUnit.Core.Test]
        [global::TUnit.Core.Arguments(LuaCompatibilityVersion.Lua51)]
        [global::TUnit.Core.Arguments(LuaCompatibilityVersion.Lua52)]
        [global::TUnit.Core.Arguments(LuaCompatibilityVersion.Lua53)]
        [global::TUnit.Core.Arguments(LuaCompatibilityVersion.Lua54)]
        [global::TUnit.Core.Arguments(LuaCompatibilityVersion.Lua55)]
        public async Task FixedObjectCallOverloadsPreserveNilAndArity(
            LuaCompatibilityVersion version
        )
        {
            Script script = new(version, CoreModulePresets.Complete);
            DynValue capture = script.DoString(
                "return function(...) return select('#', ...), ... end"
            );

            DynValue oneArgResult = script.Call(capture, (object)null);
            await Assert.That(oneArgResult.Type).IsEqualTo(DataType.Tuple).ConfigureAwait(false);
            await Assert.That(oneArgResult.Tuple.Length).IsEqualTo(2).ConfigureAwait(false);
            await Assert.That(oneArgResult.Tuple[0].Number).IsEqualTo(1d).ConfigureAwait(false);
            await Assert
                .That(oneArgResult.Tuple[1].Type)
                .IsEqualTo(DataType.Nil)
                .ConfigureAwait(false);

            DynValue threeArgResult = script.Call(capture, (object)null, "value", 42);
            await Assert.That(threeArgResult.Type).IsEqualTo(DataType.Tuple).ConfigureAwait(false);
            await Assert.That(threeArgResult.Tuple.Length).IsEqualTo(4).ConfigureAwait(false);
            await Assert.That(threeArgResult.Tuple[0].Number).IsEqualTo(3d).ConfigureAwait(false);
            await Assert
                .That(threeArgResult.Tuple[1].Type)
                .IsEqualTo(DataType.Nil)
                .ConfigureAwait(false);
            await Assert
                .That(threeArgResult.Tuple[2].String)
                .IsEqualTo("value")
                .ConfigureAwait(false);
            await Assert.That(threeArgResult.Tuple[3].Number).IsEqualTo(42d).ConfigureAwait(false);

            DynValue fourArgResult = script.Call(capture, (object)null, "value", 42, true);
            await Assert.That(fourArgResult.Type).IsEqualTo(DataType.Tuple).ConfigureAwait(false);
            await Assert.That(fourArgResult.Tuple.Length).IsEqualTo(5).ConfigureAwait(false);
            await Assert.That(fourArgResult.Tuple[0].Number).IsEqualTo(4d).ConfigureAwait(false);
            await Assert
                .That(fourArgResult.Tuple[1].Type)
                .IsEqualTo(DataType.Nil)
                .ConfigureAwait(false);
            await Assert
                .That(fourArgResult.Tuple[2].String)
                .IsEqualTo("value")
                .ConfigureAwait(false);
            await Assert.That(fourArgResult.Tuple[3].Number).IsEqualTo(42d).ConfigureAwait(false);
            await Assert.That(fourArgResult.Tuple[4].Boolean).IsTrue().ConfigureAwait(false);

            DynValue sixArgResult = script.Call(capture, (object)null, "value", 42, true, 5d, 6d);
            await Assert.That(sixArgResult.Type).IsEqualTo(DataType.Tuple).ConfigureAwait(false);
            await Assert.That(sixArgResult.Tuple.Length).IsEqualTo(7).ConfigureAwait(false);
            await Assert.That(sixArgResult.Tuple[0].Number).IsEqualTo(6d).ConfigureAwait(false);
            await Assert
                .That(sixArgResult.Tuple[1].Type)
                .IsEqualTo(DataType.Nil)
                .ConfigureAwait(false);
            await Assert
                .That(sixArgResult.Tuple[2].String)
                .IsEqualTo("value")
                .ConfigureAwait(false);
            await Assert.That(sixArgResult.Tuple[3].Number).IsEqualTo(42d).ConfigureAwait(false);
            await Assert.That(sixArgResult.Tuple[4].Boolean).IsTrue().ConfigureAwait(false);
            await Assert.That(sixArgResult.Tuple[5].Number).IsEqualTo(5d).ConfigureAwait(false);
            await Assert.That(sixArgResult.Tuple[6].Number).IsEqualTo(6d).ConfigureAwait(false);

            DynValue sevenArgResult = script.Call(
                capture,
                (object)null,
                "value",
                42,
                true,
                5d,
                6d,
                7d
            );
            await Assert.That(sevenArgResult.Type).IsEqualTo(DataType.Tuple).ConfigureAwait(false);
            await Assert.That(sevenArgResult.Tuple.Length).IsEqualTo(8).ConfigureAwait(false);
            await Assert.That(sevenArgResult.Tuple[0].Number).IsEqualTo(7d).ConfigureAwait(false);
            await Assert
                .That(sevenArgResult.Tuple[1].Type)
                .IsEqualTo(DataType.Nil)
                .ConfigureAwait(false);
            await Assert
                .That(sevenArgResult.Tuple[2].String)
                .IsEqualTo("value")
                .ConfigureAwait(false);
            await Assert.That(sevenArgResult.Tuple[3].Number).IsEqualTo(42d).ConfigureAwait(false);
            await Assert.That(sevenArgResult.Tuple[4].Boolean).IsTrue().ConfigureAwait(false);
            await Assert.That(sevenArgResult.Tuple[5].Number).IsEqualTo(5d).ConfigureAwait(false);
            await Assert.That(sevenArgResult.Tuple[6].Number).IsEqualTo(6d).ConfigureAwait(false);
            await Assert.That(sevenArgResult.Tuple[7].Number).IsEqualTo(7d).ConfigureAwait(false);
        }

        [global::TUnit.Core.Test]
        [global::TUnit.Core.Arguments(LuaCompatibilityVersion.Lua51, 6)]
        [global::TUnit.Core.Arguments(LuaCompatibilityVersion.Lua51, 7)]
        [global::TUnit.Core.Arguments(LuaCompatibilityVersion.Lua52, 6)]
        [global::TUnit.Core.Arguments(LuaCompatibilityVersion.Lua52, 7)]
        [global::TUnit.Core.Arguments(LuaCompatibilityVersion.Lua53, 6)]
        [global::TUnit.Core.Arguments(LuaCompatibilityVersion.Lua53, 7)]
        [global::TUnit.Core.Arguments(LuaCompatibilityVersion.Lua54, 6)]
        [global::TUnit.Core.Arguments(LuaCompatibilityVersion.Lua54, 7)]
        [global::TUnit.Core.Arguments(LuaCompatibilityVersion.Lua55, 6)]
        [global::TUnit.Core.Arguments(LuaCompatibilityVersion.Lua55, 7)]
        public async Task FixedSixAndSevenDynValueCallOverloadsPreserveArityAndOrder(
            LuaCompatibilityVersion version,
            int arity
        )
        {
            Script script = new(version, CoreModulePresets.Complete);
            DynValue capture = script.DoString(
                "return function(...) return select('#', ...), ... end"
            );
            DynValue[] args = CreateSequentialArguments(arity);

            DynValue result = CallFunctionWithFixedArguments(script, capture, args);

            await Assert.That(result.Type).IsEqualTo(DataType.Tuple).ConfigureAwait(false);
            await Assert.That(result.Tuple.Length).IsEqualTo(arity + 1).ConfigureAwait(false);
            await Assert
                .That(result.Tuple[0].Number)
                .IsEqualTo((double)arity)
                .ConfigureAwait(false);
            for (int i = 0; i < arity; i++)
            {
                await Assert
                    .That(result.Tuple[i + 1].Number)
                    .IsEqualTo(i + 1d)
                    .ConfigureAwait(false);
            }
        }

        [global::TUnit.Core.Test]
        [global::TUnit.Core.Arguments(LuaCompatibilityVersion.Lua51)]
        [global::TUnit.Core.Arguments(LuaCompatibilityVersion.Lua52)]
        [global::TUnit.Core.Arguments(LuaCompatibilityVersion.Lua53)]
        [global::TUnit.Core.Arguments(LuaCompatibilityVersion.Lua54)]
        [global::TUnit.Core.Arguments(LuaCompatibilityVersion.Lua55)]
        public async Task DynValueCallOverloadsPreserveNullArgumentsAsNil(
            LuaCompatibilityVersion version
        )
        {
            Script script = new(version, CoreModulePresets.Complete);
            DynValue capture = script.DoString(
                "return function(...) return select('#', ...), ... end"
            );

            DynValue fixedResult = script.Call(capture, (DynValue)null, DynValue.NewString("tail"));
            await Assert.That(fixedResult.Type).IsEqualTo(DataType.Tuple).ConfigureAwait(false);
            await Assert.That(fixedResult.Tuple.Length).IsEqualTo(3).ConfigureAwait(false);
            await Assert.That(fixedResult.Tuple[0].Number).IsEqualTo(2d).ConfigureAwait(false);
            await Assert
                .That(fixedResult.Tuple[1].Type)
                .IsEqualTo(DataType.Nil)
                .ConfigureAwait(false);
            await Assert.That(fixedResult.Tuple[2].String).IsEqualTo("tail").ConfigureAwait(false);

            DynValue arrayResult = script.Call(
                capture,
                new DynValue[] { null, DynValue.NewString("middle"), null }
            );
            await Assert.That(arrayResult.Type).IsEqualTo(DataType.Tuple).ConfigureAwait(false);
            await Assert.That(arrayResult.Tuple.Length).IsEqualTo(4).ConfigureAwait(false);
            await Assert.That(arrayResult.Tuple[0].Number).IsEqualTo(3d).ConfigureAwait(false);
            await Assert
                .That(arrayResult.Tuple[1].Type)
                .IsEqualTo(DataType.Nil)
                .ConfigureAwait(false);
            await Assert
                .That(arrayResult.Tuple[2].String)
                .IsEqualTo("middle")
                .ConfigureAwait(false);
            await Assert
                .That(arrayResult.Tuple[3].Type)
                .IsEqualTo(DataType.Nil)
                .ConfigureAwait(false);

            DynValue tupleResult = script.Call(
                capture,
                DynValue.NewString("head"),
                DynValue.NewTuple(null, DynValue.NewString("tail"))
            );
            await Assert.That(tupleResult.Type).IsEqualTo(DataType.Tuple).ConfigureAwait(false);
            await Assert.That(tupleResult.Tuple.Length).IsEqualTo(4).ConfigureAwait(false);
            await Assert.That(tupleResult.Tuple[0].Number).IsEqualTo(3d).ConfigureAwait(false);
            await Assert.That(tupleResult.Tuple[1].String).IsEqualTo("head").ConfigureAwait(false);
            await Assert
                .That(tupleResult.Tuple[2].Type)
                .IsEqualTo(DataType.Nil)
                .ConfigureAwait(false);
            await Assert.That(tupleResult.Tuple[3].String).IsEqualTo("tail").ConfigureAwait(false);
        }

        [global::TUnit.Core.Test]
        [global::TUnit.Core.Arguments(LuaCompatibilityVersion.Lua51)]
        [global::TUnit.Core.Arguments(LuaCompatibilityVersion.Lua52)]
        [global::TUnit.Core.Arguments(LuaCompatibilityVersion.Lua53)]
        [global::TUnit.Core.Arguments(LuaCompatibilityVersion.Lua54)]
        [global::TUnit.Core.Arguments(LuaCompatibilityVersion.Lua55)]
        public async Task ObjectArrayCallStillUsesParamsExpansion(LuaCompatibilityVersion version)
        {
            Script script = new(version, CoreModulePresets.Complete);
            DynValue capture = script.DoString(
                "return function(...) return select('#', ...), ... end"
            );
            object[] args = new object[] { 1, 2, 3 };

            DynValue result = script.Call(capture, args);

            await AssertTupleNumbers(result, 3d, 1d, 2d, 3d).ConfigureAwait(false);
        }

        [global::TUnit.Core.Test]
        [global::TUnit.Core.Arguments(LuaCompatibilityVersion.Lua51)]
        [global::TUnit.Core.Arguments(LuaCompatibilityVersion.Lua52)]
        [global::TUnit.Core.Arguments(LuaCompatibilityVersion.Lua53)]
        [global::TUnit.Core.Arguments(LuaCompatibilityVersion.Lua54)]
        [global::TUnit.Core.Arguments(LuaCompatibilityVersion.Lua55)]
        public async Task CastObjectArrayCallPassesSingleTableArgument(
            LuaCompatibilityVersion version
        )
        {
            Script script = new(version, CoreModulePresets.Complete);
            DynValue capture = script.DoString(
                "return function(value) return type(value), #value, value[1], value[2] end"
            );
            object[] args = new object[] { 1, 2 };

            DynValue result = script.Call(capture, (object)args);

            await Assert.That(result.Type).IsEqualTo(DataType.Tuple).ConfigureAwait(false);
            await Assert.That(result.Tuple.Length).IsEqualTo(4).ConfigureAwait(false);
            await Assert.That(result.Tuple[0].String).IsEqualTo("table").ConfigureAwait(false);
            await Assert.That(result.Tuple[1].Number).IsEqualTo(2d).ConfigureAwait(false);
            await Assert.That(result.Tuple[2].Number).IsEqualTo(1d).ConfigureAwait(false);
            await Assert.That(result.Tuple[3].Number).IsEqualTo(2d).ConfigureAwait(false);
        }

        [global::TUnit.Core.Test]
        [global::TUnit.Core.Arguments(LuaCompatibilityVersion.Lua51)]
        [global::TUnit.Core.Arguments(LuaCompatibilityVersion.Lua52)]
        [global::TUnit.Core.Arguments(LuaCompatibilityVersion.Lua53)]
        [global::TUnit.Core.Arguments(LuaCompatibilityVersion.Lua54)]
        [global::TUnit.Core.Arguments(LuaCompatibilityVersion.Lua55)]
        public async Task FixedObjectCallOverloadsValidateFunctionBeforeArguments(
            LuaCompatibilityVersion version
        )
        {
            Script script = new(version, CoreModulePresets.Complete);

            ArgumentNullException exception = Assert.Throws<ArgumentNullException>(() =>
                script.Call((DynValue)null, new UnregisteredHostObject())
            );

            await Assert.That(exception.ParamName).IsEqualTo("function").ConfigureAwait(false);
        }

        [global::TUnit.Core.Test]
        [global::TUnit.Core.Arguments(LuaCompatibilityVersion.Lua51)]
        [global::TUnit.Core.Arguments(LuaCompatibilityVersion.Lua52)]
        [global::TUnit.Core.Arguments(LuaCompatibilityVersion.Lua53)]
        [global::TUnit.Core.Arguments(LuaCompatibilityVersion.Lua54)]
        [global::TUnit.Core.Arguments(LuaCompatibilityVersion.Lua55)]
        public async Task FixedDynValueCallOverloadsPreserveTupleExpansion(
            LuaCompatibilityVersion version
        )
        {
            Script script = new(version, CoreModulePresets.Complete);
            DynValue capture = script.DoString(
                "return function(...) return select('#', ...), ... end"
            );

            DynValue oneArgResult = script.Call(
                capture,
                DynValue.NewTuple(DynValue.NewNumber(1), DynValue.NewNumber(2))
            );
            await AssertTupleNumbers(oneArgResult, 2d, 1d, 2d).ConfigureAwait(false);

            DynValue twoArgResult = script.Call(
                capture,
                DynValue.NewTuple(DynValue.NewNumber(1), DynValue.NewNumber(2)),
                DynValue.NewNumber(3)
            );
            await AssertTupleNumbers(twoArgResult, 2d, 1d, 3d).ConfigureAwait(false);

            DynValue nestedTail = DynValue.NewTuple(
                DynValue.NewNumber(3),
                DynValue.NewTuple(DynValue.NewNumber(4), DynValue.NewNumber(5))
            );
            DynValue threeArgResult = script.Call(
                capture,
                DynValue.NewNumber(1),
                DynValue.NewNumber(2),
                nestedTail
            );
            await AssertTupleNumbers(threeArgResult, 5d, 1d, 2d, 3d, 4d, 5d).ConfigureAwait(false);

            DynValue fourArgTail = DynValue.NewTuple(
                DynValue.NewNumber(4),
                DynValue.NewTuple(DynValue.NewNumber(5), DynValue.NewNumber(6))
            );
            DynValue fourArgResult = script.Call(
                capture,
                DynValue.NewNumber(1),
                DynValue.NewNumber(2),
                DynValue.NewNumber(3),
                fourArgTail
            );
            await AssertTupleNumbers(fourArgResult, 6d, 1d, 2d, 3d, 4d, 5d, 6d)
                .ConfigureAwait(false);

            DynValue nonTrailingTuple = DynValue.NewTuple(
                DynValue.NewNumber(4),
                DynValue.NewNumber(5)
            );
            DynValue trailingTuple = DynValue.NewTuple(
                DynValue.NewNumber(6),
                DynValue.NewTuple(DynValue.NewNumber(7), DynValue.NewNumber(8))
            );
            DynValue mixedTupleResult = script.Call(
                capture,
                DynValue.NewTuple(DynValue.NewNumber(1), DynValue.NewNumber(2)),
                DynValue.NewNumber(3),
                nonTrailingTuple,
                trailingTuple
            );
            await AssertTupleNumbers(mixedTupleResult, 6d, 1d, 3d, 4d, 6d, 7d, 8d)
                .ConfigureAwait(false);
        }

        [global::TUnit.Core.Test]
        [global::TUnit.Core.Arguments(LuaCompatibilityVersion.Lua51)]
        [global::TUnit.Core.Arguments(LuaCompatibilityVersion.Lua52)]
        [global::TUnit.Core.Arguments(LuaCompatibilityVersion.Lua53)]
        [global::TUnit.Core.Arguments(LuaCompatibilityVersion.Lua54)]
        [global::TUnit.Core.Arguments(LuaCompatibilityVersion.Lua55)]
        public async Task FixedDynValueCallOverloadsTrimTrailingVoidForScriptFunctions(
            LuaCompatibilityVersion version
        )
        {
            Script script = new(version, CoreModulePresets.Complete);
            DynValue capture = script.DoString(
                "return function(...) return select('#', ...), ... end"
            );

            DynValue oneVoid = script.Call(capture, DynValue.Void);
            DynValue twoVoid = script.Call(capture, DynValue.NewNumber(1), DynValue.Void);
            DynValue threeVoid = script.Call(
                capture,
                DynValue.NewNumber(1),
                DynValue.NewNumber(2),
                DynValue.Void
            );
            DynValue fourVoid = script.Call(
                capture,
                DynValue.NewNumber(1),
                DynValue.NewNumber(2),
                DynValue.NewNumber(3),
                DynValue.Void
            );
            DynValue trailingTupleVoid = script.Call(
                capture,
                DynValue.NewNumber(1),
                DynValue.NewTuple(DynValue.NewNumber(2), DynValue.Void)
            );

            await Assert.That(oneVoid.Type).IsEqualTo(DataType.Number).ConfigureAwait(false);
            await Assert.That(oneVoid.Number).IsEqualTo(0d).ConfigureAwait(false);
            await AssertTupleNumbers(twoVoid, 1d, 1d).ConfigureAwait(false);
            await AssertTupleNumbers(threeVoid, 2d, 1d, 2d).ConfigureAwait(false);
            await AssertTupleNumbers(fourVoid, 3d, 1d, 2d, 3d).ConfigureAwait(false);
            await AssertTupleNumbers(trailingTupleVoid, 2d, 1d, 2d).ConfigureAwait(false);
        }

        [global::TUnit.Core.Test]
        [global::TUnit.Core.Arguments(LuaCompatibilityVersion.Lua51)]
        [global::TUnit.Core.Arguments(LuaCompatibilityVersion.Lua52)]
        [global::TUnit.Core.Arguments(LuaCompatibilityVersion.Lua53)]
        [global::TUnit.Core.Arguments(LuaCompatibilityVersion.Lua54)]
        [global::TUnit.Core.Arguments(LuaCompatibilityVersion.Lua55)]
        public async Task CallObjectOverloadInvokesClosureAndConvertsArguments(
            LuaCompatibilityVersion version
        )
        {
            Script script = new(version, CoreModulePresets.Complete);
            script.DoString("function mul(a, b, c, d) return a * b + c + d end");
            object closure = script.Globals.Get("mul").Function;

            DynValue result = script.Call(closure, 6, 7, -1, 1);

            await Assert.That(result.Number).IsEqualTo(42d).ConfigureAwait(false);
        }

        [global::TUnit.Core.Test]
        [global::TUnit.Core.Arguments(LuaCompatibilityVersion.Lua51)]
        [global::TUnit.Core.Arguments(LuaCompatibilityVersion.Lua52)]
        [global::TUnit.Core.Arguments(LuaCompatibilityVersion.Lua53)]
        [global::TUnit.Core.Arguments(LuaCompatibilityVersion.Lua54)]
        [global::TUnit.Core.Arguments(LuaCompatibilityVersion.Lua55)]
        public async Task CallObjectOverloadInvokesDelegateCallback(LuaCompatibilityVersion version)
        {
            Script script = new(version, CoreModulePresets.Complete);
            Func<ScriptExecutionContext, CallbackArguments, DynValue> callback = (ctx, args) =>
                DynValue.NewNumber(args[0].Number * 2d);

            DynValue result = script.Call(callback, 21);

            await Assert.That(result.Number).IsEqualTo(42d).ConfigureAwait(false);
        }

        [global::TUnit.Core.Test]
        [global::TUnit.Core.Arguments(LuaCompatibilityVersion.Lua51)]
        [global::TUnit.Core.Arguments(LuaCompatibilityVersion.Lua52)]
        [global::TUnit.Core.Arguments(LuaCompatibilityVersion.Lua53)]
        [global::TUnit.Core.Arguments(LuaCompatibilityVersion.Lua54)]
        [global::TUnit.Core.Arguments(LuaCompatibilityVersion.Lua55)]
        public async Task CallObjectOverloadRejectsNonCallableValues(
            LuaCompatibilityVersion version
        )
        {
            Script script = new(version, CoreModulePresets.Complete);

            ArgumentException exception = Assert.Throws<ArgumentException>(() =>
                script.Call((object)"not callable")
            );

            await Assert
                .That(exception.Message)
                .Contains("__call metamethod")
                .ConfigureAwait(false);
        }

        [global::TUnit.Core.Test]
        [global::TUnit.Core.Arguments(LuaCompatibilityVersion.Lua51)]
        [global::TUnit.Core.Arguments(LuaCompatibilityVersion.Lua52)]
        [global::TUnit.Core.Arguments(LuaCompatibilityVersion.Lua53)]
        [global::TUnit.Core.Arguments(LuaCompatibilityVersion.Lua54)]
        [global::TUnit.Core.Arguments(LuaCompatibilityVersion.Lua55)]
        public async Task CallObjectOverloadThrowsWhenFunctionNull(LuaCompatibilityVersion version)
        {
            Script script = new(version, CoreModulePresets.Complete);

            ArgumentException exception = Assert.Throws<ArgumentException>(() =>
                script.Call((object)null)
            );

            await Assert
                .That(exception.Message)
                .Contains("__call metamethod")
                .ConfigureAwait(false);
        }

        [global::TUnit.Core.Test]
        [global::TUnit.Core.Arguments(LuaCompatibilityVersion.Lua51)]
        [global::TUnit.Core.Arguments(LuaCompatibilityVersion.Lua52)]
        [global::TUnit.Core.Arguments(LuaCompatibilityVersion.Lua53)]
        [global::TUnit.Core.Arguments(LuaCompatibilityVersion.Lua54)]
        [global::TUnit.Core.Arguments(LuaCompatibilityVersion.Lua55)]
        public async Task CreateCoroutineValidatesInputs(LuaCompatibilityVersion version)
        {
            Script script = new(version, CoreModulePresets.Complete);
            DynValue callback = DynValue.NewCallback((_, _) => DynValue.NewString("done"));

            DynValue coroutine = script.CreateCoroutine(callback);
            await Assert.That(coroutine.Type).IsEqualTo(DataType.Thread).ConfigureAwait(false);

            ArgumentException exception = Assert.Throws<ArgumentException>(() =>
                script.CreateCoroutine(DynValue.NewNumber(1))
            );

            await Assert
                .That(exception.Message)
                .Contains("DataType.Function")
                .ConfigureAwait(false);
        }

        [global::TUnit.Core.Test]
        [global::TUnit.Core.Arguments(LuaCompatibilityVersion.Lua51)]
        [global::TUnit.Core.Arguments(LuaCompatibilityVersion.Lua52)]
        [global::TUnit.Core.Arguments(LuaCompatibilityVersion.Lua53)]
        [global::TUnit.Core.Arguments(LuaCompatibilityVersion.Lua54)]
        [global::TUnit.Core.Arguments(LuaCompatibilityVersion.Lua55)]
        public async Task CreateCoroutineThrowsWhenFunctionNull(LuaCompatibilityVersion version)
        {
            Script script = new(version, CoreModulePresets.Complete);

            ArgumentNullException exception = Assert.Throws<ArgumentNullException>(() =>
                script.CreateCoroutine((DynValue)null)
            );

            await Assert.That(exception.ParamName).IsEqualTo("function").ConfigureAwait(false);
        }

        [global::TUnit.Core.Test]
        [global::TUnit.Core.Arguments(LuaCompatibilityVersion.Lua51)]
        [global::TUnit.Core.Arguments(LuaCompatibilityVersion.Lua52)]
        [global::TUnit.Core.Arguments(LuaCompatibilityVersion.Lua53)]
        [global::TUnit.Core.Arguments(LuaCompatibilityVersion.Lua54)]
        [global::TUnit.Core.Arguments(LuaCompatibilityVersion.Lua55)]
        public async Task RequireModuleWarnsWhenBit32NotSupported(LuaCompatibilityVersion version)
        {
            StubScriptLoader loader = new()
            {
                ModuleSource = "return function() return 'bit32' end",
            };
            List<string> messages = new();
            ScriptOptions options = new() { ScriptLoader = loader, DebugPrint = messages.Add };
            Script script = new(CoreModulePresets.Complete, options);

            DynValue result = script.RequireModule("bit32");

            await Assert.That(loader.ResolveCalls).IsEqualTo(1).ConfigureAwait(false);
            await Assert.That(loader.LoadCalls).IsEqualTo(1).ConfigureAwait(false);
            await Assert.That(messages.Count).IsEqualTo(1).ConfigureAwait(false);
            await Assert.That(messages[0]).Contains("require('bit32')").ConfigureAwait(false);
            await Assert.That(result.Type).IsEqualTo(DataType.Function).ConfigureAwait(false);
        }

        [global::TUnit.Core.Test]
        [global::TUnit.Core.Arguments(LuaCompatibilityVersion.Lua51)]
        [global::TUnit.Core.Arguments(LuaCompatibilityVersion.Lua52)]
        [global::TUnit.Core.Arguments(LuaCompatibilityVersion.Lua53)]
        [global::TUnit.Core.Arguments(LuaCompatibilityVersion.Lua54)]
        [global::TUnit.Core.Arguments(LuaCompatibilityVersion.Lua55)]
        public async Task RequireModuleThrowsWhenModuleMissing(LuaCompatibilityVersion version)
        {
            StubScriptLoader loader = new() { ResolveReturnsNull = true };
            ScriptOptions options = new() { ScriptLoader = loader };
            Script script = new(CoreModulePresets.Complete, options);

            ScriptRuntimeException exception = Assert.Throws<ScriptRuntimeException>(() =>
                script.RequireModule("missing")
            );

            await Assert
                .That(exception.Message)
                .Contains("module 'missing' not found")
                .ConfigureAwait(false);
        }

        [global::TUnit.Core.Test]
        [global::TUnit.Core.Arguments(LuaCompatibilityVersion.Lua51)]
        [global::TUnit.Core.Arguments(LuaCompatibilityVersion.Lua52)]
        [global::TUnit.Core.Arguments(LuaCompatibilityVersion.Lua53)]
        [global::TUnit.Core.Arguments(LuaCompatibilityVersion.Lua54)]
        [global::TUnit.Core.Arguments(LuaCompatibilityVersion.Lua55)]
        public async Task RequireModuleWarnsOnlyOnceForBit32(LuaCompatibilityVersion version)
        {
            StubScriptLoader loader = new() { ModuleSource = "return function() end" };
            List<string> messages = new();
            ScriptOptions options = new() { ScriptLoader = loader, DebugPrint = messages.Add };
            Script script = new(CoreModulePresets.Complete, options);

            script.RequireModule("bit32");
            script.RequireModule("bit32");

            await Assert.That(messages.Count).IsEqualTo(1).ConfigureAwait(false);
        }

        [global::TUnit.Core.Test]
        [global::TUnit.Core.Arguments(LuaCompatibilityVersion.Lua51)]
        [global::TUnit.Core.Arguments(LuaCompatibilityVersion.Lua52)]
        [global::TUnit.Core.Arguments(LuaCompatibilityVersion.Lua53)]
        [global::TUnit.Core.Arguments(LuaCompatibilityVersion.Lua54)]
        [global::TUnit.Core.Arguments(LuaCompatibilityVersion.Lua55)]
        public async Task RequireModuleDoesNotWarnWhenProfileSupportsBit32(
            LuaCompatibilityVersion version
        )
        {
            StubScriptLoader loader = new() { ModuleSource = "return function() end" };
            List<string> messages = new();
            ScriptOptions options = new()
            {
                ScriptLoader = loader,
                DebugPrint = messages.Add,
                CompatibilityVersion = LuaCompatibilityVersion.Lua52,
            };
            Script script = new(CoreModulePresets.Complete, options);

            script.RequireModule("bit32");

            await Assert.That(messages.Count).IsZero().ConfigureAwait(false);
        }

        [global::TUnit.Core.Test]
        [global::TUnit.Core.Arguments(LuaCompatibilityVersion.Lua51)]
        [global::TUnit.Core.Arguments(LuaCompatibilityVersion.Lua52)]
        [global::TUnit.Core.Arguments(LuaCompatibilityVersion.Lua53)]
        [global::TUnit.Core.Arguments(LuaCompatibilityVersion.Lua54)]
        [global::TUnit.Core.Arguments(LuaCompatibilityVersion.Lua55)]
        public async Task RequireModuleUsesProvidedGlobalContext(LuaCompatibilityVersion version)
        {
            StubScriptLoader loader = new();
            Script script = new(
                CoreModulePresets.Complete,
                new ScriptOptions { ScriptLoader = loader }
            );
            Table customGlobals = new(script);

            script.RequireModule("custom", customGlobals);

            await Assert.That(loader.ResolveCalls).IsEqualTo(1).ConfigureAwait(false);
            await Assert
                .That(loader.LastGlobalContext)
                .IsSameReferenceAs(customGlobals)
                .ConfigureAwait(false);
        }

        [global::TUnit.Core.Test]
        [global::TUnit.Core.Arguments(LuaCompatibilityVersion.Lua51)]
        [global::TUnit.Core.Arguments(LuaCompatibilityVersion.Lua52)]
        [global::TUnit.Core.Arguments(LuaCompatibilityVersion.Lua53)]
        [global::TUnit.Core.Arguments(LuaCompatibilityVersion.Lua54)]
        [global::TUnit.Core.Arguments(LuaCompatibilityVersion.Lua55)]
        public async Task RequireModuleDefaultsToScriptGlobals(LuaCompatibilityVersion version)
        {
            StubScriptLoader loader = new();
            Script script = new(
                CoreModulePresets.Complete,
                new ScriptOptions { ScriptLoader = loader }
            );

            script.RequireModule("custom");

            await Assert
                .That(loader.LastGlobalContext)
                .IsSameReferenceAs(script.Globals)
                .ConfigureAwait(false);
        }

        [global::TUnit.Core.Test]
        [global::TUnit.Core.Arguments(LuaCompatibilityVersion.Lua51)]
        [global::TUnit.Core.Arguments(LuaCompatibilityVersion.Lua52)]
        [global::TUnit.Core.Arguments(LuaCompatibilityVersion.Lua53)]
        [global::TUnit.Core.Arguments(LuaCompatibilityVersion.Lua54)]
        [global::TUnit.Core.Arguments(LuaCompatibilityVersion.Lua55)]
        public async Task RequireModuleThrowsWhenGlobalContextOwnedByDifferentScript(
            LuaCompatibilityVersion version
        )
        {
            StubScriptLoader loader = new();
            Script script = new(
                CoreModulePresets.Complete,
                new ScriptOptions { ScriptLoader = loader }
            );
            Script foreignScript = new(CoreModulePresets.Complete);
            Table foreignGlobals = new(foreignScript);

            ScriptRuntimeException exception = Assert.Throws<ScriptRuntimeException>(() =>
                script.RequireModule("custom", foreignGlobals)
            );

            await Assert
                .That(exception.Message)
                .Contains("different scripts")
                .ConfigureAwait(false);
        }

        [global::TUnit.Core.Test]
        [global::TUnit.Core.Arguments(LuaCompatibilityVersion.Lua51)]
        [global::TUnit.Core.Arguments(LuaCompatibilityVersion.Lua52)]
        [global::TUnit.Core.Arguments(LuaCompatibilityVersion.Lua53)]
        [global::TUnit.Core.Arguments(LuaCompatibilityVersion.Lua54)]
        [global::TUnit.Core.Arguments(LuaCompatibilityVersion.Lua55)]
        public async Task CallRejectsValuesOwnedByDifferentScripts(LuaCompatibilityVersion version)
        {
            Script scriptA = new(CoreModulePresets.Complete);
            Script scriptB = new(CoreModulePresets.Complete);

            DynValue foreignTable = scriptA.DoString("return {}");
            scriptB.DoString("function echo(value) return value end");

            ScriptRuntimeException exception = Assert.Throws<ScriptRuntimeException>(() =>
                scriptB.Call(scriptB.Globals.Get("echo"), foreignTable)
            );

            await Assert
                .That(exception.Message)
                .Contains("different scripts")
                .ConfigureAwait(false);
        }

        [global::TUnit.Core.Test]
        [global::TUnit.Core.Arguments(LuaCompatibilityVersion.Lua51)]
        [global::TUnit.Core.Arguments(LuaCompatibilityVersion.Lua52)]
        [global::TUnit.Core.Arguments(LuaCompatibilityVersion.Lua53)]
        [global::TUnit.Core.Arguments(LuaCompatibilityVersion.Lua54)]
        [global::TUnit.Core.Arguments(LuaCompatibilityVersion.Lua55)]
        public async Task FourDynValueCallRejectsFourthValueOwnedByDifferentScript(
            LuaCompatibilityVersion version
        )
        {
            Script scriptA = new(CoreModulePresets.Complete);
            Script scriptB = new(CoreModulePresets.Complete);

            DynValue foreignTable = scriptA.DoString("return {}");
            scriptB.DoString("function echo(a, b, c, d) return d end");

            ScriptRuntimeException exception = Assert.Throws<ScriptRuntimeException>(() =>
                scriptB.Call(
                    scriptB.Globals.Get("echo"),
                    DynValue.Nil,
                    DynValue.Nil,
                    DynValue.Nil,
                    foreignTable
                )
            );

            await Assert
                .That(exception.Message)
                .Contains("different scripts")
                .ConfigureAwait(false);
        }

        [global::TUnit.Core.Test]
        [global::TUnit.Core.Arguments(LuaCompatibilityVersion.Lua51)]
        [global::TUnit.Core.Arguments(LuaCompatibilityVersion.Lua52)]
        [global::TUnit.Core.Arguments(LuaCompatibilityVersion.Lua53)]
        [global::TUnit.Core.Arguments(LuaCompatibilityVersion.Lua54)]
        [global::TUnit.Core.Arguments(LuaCompatibilityVersion.Lua55)]
        public async Task CallObjectOverloadRejectsForeignClosure(LuaCompatibilityVersion version)
        {
            Script scriptA = new(CoreModulePresets.Complete);
            scriptA.DoString("function noop() return 1 end");
            object foreignClosure = scriptA.Globals.Get("noop").Function;

            Script scriptB = new(CoreModulePresets.Complete);

            ScriptRuntimeException exception = Assert.Throws<ScriptRuntimeException>(() =>
                scriptB.Call(foreignClosure)
            );

            await Assert
                .That(exception.Message)
                .Contains("different scripts")
                .ConfigureAwait(false);
        }

        [global::TUnit.Core.Test]
        [global::TUnit.Core.Arguments(LuaCompatibilityVersion.Lua51)]
        [global::TUnit.Core.Arguments(LuaCompatibilityVersion.Lua52)]
        [global::TUnit.Core.Arguments(LuaCompatibilityVersion.Lua53)]
        [global::TUnit.Core.Arguments(LuaCompatibilityVersion.Lua54)]
        [global::TUnit.Core.Arguments(LuaCompatibilityVersion.Lua55)]
        public async Task CreateCoroutineRejectsFunctionsOwnedByDifferentScripts(
            LuaCompatibilityVersion version
        )
        {
            Script scriptA = new(CoreModulePresets.Complete);
            Script scriptB = new(CoreModulePresets.Complete);
            DynValue foreignFunction = scriptA.DoString("return function() end");

            ScriptRuntimeException exception = Assert.Throws<ScriptRuntimeException>(() =>
                scriptB.CreateCoroutine(foreignFunction)
            );

            await Assert
                .That(exception.Message)
                .Contains("different scripts")
                .ConfigureAwait(false);
        }

        [global::TUnit.Core.Test]
        [global::TUnit.Core.Arguments(LuaCompatibilityVersion.Lua51)]
        [global::TUnit.Core.Arguments(LuaCompatibilityVersion.Lua52)]
        [global::TUnit.Core.Arguments(LuaCompatibilityVersion.Lua53)]
        [global::TUnit.Core.Arguments(LuaCompatibilityVersion.Lua54)]
        [global::TUnit.Core.Arguments(LuaCompatibilityVersion.Lua55)]
        public async Task CreateCoroutineObjectOverloadUsesClosure(LuaCompatibilityVersion version)
        {
            Script script = new(version, CoreModulePresets.Complete);
            script.DoString(
                @"
                function generator()
                    coroutine.yield(5)
                    return 6
                end
            "
            );

            object closure = script.Globals.Get("generator").Function;
            DynValue coroutine = script.CreateCoroutine(closure);

            DynValue first = coroutine.Coroutine.Resume();
            DynValue second = coroutine.Coroutine.Resume();

            await Assert.That(first.Number).IsEqualTo(5d).ConfigureAwait(false);
            await Assert.That(second.Number).IsEqualTo(6d).ConfigureAwait(false);
        }

        [global::TUnit.Core.Test]
        [global::TUnit.Core.Arguments(LuaCompatibilityVersion.Lua51)]
        [global::TUnit.Core.Arguments(LuaCompatibilityVersion.Lua52)]
        [global::TUnit.Core.Arguments(LuaCompatibilityVersion.Lua53)]
        [global::TUnit.Core.Arguments(LuaCompatibilityVersion.Lua54)]
        [global::TUnit.Core.Arguments(LuaCompatibilityVersion.Lua55)]
        public async Task CreateCoroutineObjectOverloadSupportsDelegates(
            LuaCompatibilityVersion version
        )
        {
            Script script = new(version, CoreModulePresets.Complete);
            Func<ScriptExecutionContext, CallbackArguments, DynValue> callback = (ctx, _) =>
                DynValue.NewNumber(99d);

            DynValue coroutineValue = script.CreateCoroutine(callback);
            coroutineValue.Coroutine.OwnerScript = script;
            ScriptExecutionContext context = script.CreateDynamicExecutionContext();

            DynValue result = coroutineValue.Coroutine.Resume(context);

            await Assert.That(result.Number).IsEqualTo(99d).ConfigureAwait(false);
            await Assert
                .That(coroutineValue.Coroutine.State)
                .IsEqualTo(CoroutineState.Dead)
                .ConfigureAwait(false);
        }

        [global::TUnit.Core.Test]
        [global::TUnit.Core.Arguments(LuaCompatibilityVersion.Lua51)]
        [global::TUnit.Core.Arguments(LuaCompatibilityVersion.Lua52)]
        [global::TUnit.Core.Arguments(LuaCompatibilityVersion.Lua53)]
        [global::TUnit.Core.Arguments(LuaCompatibilityVersion.Lua54)]
        [global::TUnit.Core.Arguments(LuaCompatibilityVersion.Lua55)]
        public async Task CreateCoroutineObjectOverloadRejectsNonCallable(
            LuaCompatibilityVersion version
        )
        {
            Script script = new(version, CoreModulePresets.Complete);

            ArgumentException exception = Assert.Throws<ArgumentException>(() =>
                script.CreateCoroutine((object)"invalid")
            );

            await Assert
                .That(exception.Message)
                .Contains("DataType.Function")
                .ConfigureAwait(false);
        }

        [global::TUnit.Core.Test]
        [global::TUnit.Core.Arguments(LuaCompatibilityVersion.Lua51)]
        [global::TUnit.Core.Arguments(LuaCompatibilityVersion.Lua52)]
        [global::TUnit.Core.Arguments(LuaCompatibilityVersion.Lua53)]
        [global::TUnit.Core.Arguments(LuaCompatibilityVersion.Lua54)]
        [global::TUnit.Core.Arguments(LuaCompatibilityVersion.Lua55)]
        public async Task CreateCoroutineObjectOverloadRejectsForeignClosure(
            LuaCompatibilityVersion version
        )
        {
            Script scriptA = new(CoreModulePresets.Complete);
            scriptA.DoString("function noop() return 0 end");
            object foreignClosure = scriptA.Globals.Get("noop").Function;

            Script scriptB = new(CoreModulePresets.Complete);

            ScriptRuntimeException exception = Assert.Throws<ScriptRuntimeException>(() =>
                scriptB.CreateCoroutine(foreignClosure)
            );

            await Assert
                .That(exception.Message)
                .Contains("different scripts")
                .ConfigureAwait(false);
        }

        private static async Task AssertTupleNumbers(DynValue value, params double[] expected)
        {
            await Assert.That(value.Type).IsEqualTo(DataType.Tuple).ConfigureAwait(false);
            await Assert.That(value.Tuple.Length).IsEqualTo(expected.Length).ConfigureAwait(false);

            for (int i = 0; i < expected.Length; i++)
            {
                await Assert
                    .That(value.Tuple[i].Number)
                    .IsEqualTo(expected[i])
                    .ConfigureAwait(false);
            }
        }

        private static DynValue SummarizeArguments(CallbackArguments args)
        {
            double nilCount = 0d;
            double sum = 0d;

            for (int i = 0; i < args.Count; i++)
            {
                DynValue arg = args[i];
                if (arg.Type == DataType.Nil)
                {
                    nilCount++;
                }
                else
                {
                    sum += arg.Number;
                }
            }

            return DynValue.NewTuple(
                DynValue.NewNumber(args.Count),
                DynValue.NewNumber(nilCount),
                DynValue.NewNumber(sum)
            );
        }

        private static DynValue SummarizeArgumentsSkippingFirst(CallbackArguments args)
        {
            double nilCount = 0d;
            double sum = 0d;

            for (int i = 1; i < args.Count; i++)
            {
                DynValue arg = args[i];
                if (arg.Type == DataType.Nil)
                {
                    nilCount++;
                }
                else
                {
                    sum += arg.Number;
                }
            }

            return DynValue.NewTuple(
                DynValue.NewNumber(Math.Max(args.Count - 1, 0)),
                DynValue.NewNumber(nilCount),
                DynValue.NewNumber(sum)
            );
        }

        private static DynValue SummarizeArgumentsSkippingFirst(CallbackArgumentsView args)
        {
            double nilCount = 0d;
            double sum = 0d;

            for (int i = 1; i < args.Count; i++)
            {
                DynValue arg = args[i];
                if (arg.Type == DataType.Nil)
                {
                    nilCount++;
                }
                else
                {
                    sum += arg.Number;
                }
            }

            return DynValue.NewTuple(
                DynValue.NewNumber(Math.Max(args.Count - 1, 0)),
                DynValue.NewNumber(nilCount),
                DynValue.NewNumber(sum)
            );
        }

        private static async Task AssertArgumentSummary(
            DynValue value,
            double count,
            double nilCount,
            double sum
        )
        {
            await Assert.That(value.Type).IsEqualTo(DataType.Tuple).ConfigureAwait(false);
            await Assert.That(value.Tuple.Length).IsEqualTo(3).ConfigureAwait(false);
            await Assert.That(value.Tuple[0].Number).IsEqualTo(count).ConfigureAwait(false);
            await Assert.That(value.Tuple[1].Number).IsEqualTo(nilCount).ConfigureAwait(false);
            await Assert.That(value.Tuple[2].Number).IsEqualTo(sum).ConfigureAwait(false);
        }

        private static DynValue CreateTableValuedCallChain(
            Script script,
            Func<Table, Table, CallbackArguments, DynValue> callback
        )
        {
            Table target = new(script);
            Table proxy = new(script);
            Table targetMeta = new(script);
            Table proxyMeta = new(script);

            targetMeta.Set("__call", DynValue.NewTable(proxy));
            proxyMeta.Set(
                "__call",
                DynValue.NewCallback((_, args) => callback(target, proxy, args))
            );
            target.MetaTable = targetMeta;
            proxy.MetaTable = proxyMeta;

            return DynValue.NewTable(target);
        }

        private static DynValue SummarizeChainedCallArguments(
            Table target,
            Table proxy,
            CallbackArguments args
        )
        {
            double userArgumentSum = 0d;
            for (int i = 2; i < args.Count; i++)
            {
                userArgumentSum += args[i].Number;
            }

            return DynValue.NewTuple(
                DynValue.NewNumber(args.Count),
                DynValue.NewBoolean(args.Count >= 2 && ReferenceEquals(args[0].Table, proxy)),
                DynValue.NewBoolean(args.Count >= 2 && ReferenceEquals(args[1].Table, target)),
                DynValue.NewBoolean(args.IsMethodCall),
                DynValue.NewNumber(userArgumentSum)
            );
        }

        private static async Task AssertChainedCallSummary(DynValue value, int userArity)
        {
            await Assert.That(value.Type).IsEqualTo(DataType.Tuple).ConfigureAwait(false);
            await Assert.That(value.Tuple.Length).IsEqualTo(5).ConfigureAwait(false);
            await Assert
                .That(value.Tuple[0].Number)
                .IsEqualTo(userArity + 2d)
                .ConfigureAwait(false);
            await Assert.That(value.Tuple[1].Boolean).IsTrue().ConfigureAwait(false);
            await Assert.That(value.Tuple[2].Boolean).IsTrue().ConfigureAwait(false);
            await Assert.That(value.Tuple[3].Boolean).IsFalse().ConfigureAwait(false);
            await Assert
                .That(value.Tuple[4].Number)
                .IsEqualTo(userArity * (userArity + 1) / 2d)
                .ConfigureAwait(false);
        }

        private static DynValue CallLegacyCallbackWithSequentialArguments(
            Script script,
            DynValue callback,
            int arity
        )
        {
            return arity switch
            {
                0 => script.Call(callback),
                1 => script.Call(callback, DynValue.NewNumber(1)),
                2 => script.Call(callback, DynValue.NewNumber(1), DynValue.NewNumber(2)),
                3 => script.Call(
                    callback,
                    DynValue.NewNumber(1),
                    DynValue.NewNumber(2),
                    DynValue.NewNumber(3)
                ),
                4 => script.Call(
                    callback,
                    DynValue.NewNumber(1),
                    DynValue.NewNumber(2),
                    DynValue.NewNumber(3),
                    DynValue.NewNumber(4)
                ),
                5 => script.Call(
                    callback,
                    DynValue.NewNumber(1),
                    DynValue.NewNumber(2),
                    DynValue.NewNumber(3),
                    DynValue.NewNumber(4),
                    DynValue.NewNumber(5)
                ),
                6 => script.Call(
                    callback,
                    DynValue.NewNumber(1),
                    DynValue.NewNumber(2),
                    DynValue.NewNumber(3),
                    DynValue.NewNumber(4),
                    DynValue.NewNumber(5),
                    DynValue.NewNumber(6)
                ),
                7 => script.Call(
                    callback,
                    DynValue.NewNumber(1),
                    DynValue.NewNumber(2),
                    DynValue.NewNumber(3),
                    DynValue.NewNumber(4),
                    DynValue.NewNumber(5),
                    DynValue.NewNumber(6),
                    DynValue.NewNumber(7)
                ),
                _ => throw new ArgumentOutOfRangeException(nameof(arity)),
            };
        }

        private static DynValue[] CreateSequentialArguments(int arity)
        {
            DynValue[] args = new DynValue[arity];
            for (int i = 0; i < args.Length; i++)
            {
                args[i] = DynValue.NewNumber(i + 1d);
            }

            return args;
        }

        private static DynValue CallFunctionWithFixedArguments(
            Script script,
            DynValue function,
            DynValue[] args
        )
        {
            return args.Length switch
            {
                6 => script.Call(function, args[0], args[1], args[2], args[3], args[4], args[5]),
                7 => script.Call(
                    function,
                    args[0],
                    args[1],
                    args[2],
                    args[3],
                    args[4],
                    args[5],
                    args[6]
                ),
                _ => throw new ArgumentOutOfRangeException(nameof(args)),
            };
        }

        private static long MeasureNoArgumentLegacyCallbackAllocations(
            Script script,
            DynValue callback,
            int iterations
        )
        {
            long before = GC.GetAllocatedBytesForCurrentThread();
            for (int i = 0; i < iterations; i++)
            {
                script.Call(callback);
            }

            return GC.GetAllocatedBytesForCurrentThread() - before;
        }

        private static long MeasureFixedThreeArgumentLegacyCallbackAllocations(
            Script script,
            DynValue callback,
            DynValue first,
            DynValue second,
            DynValue third,
            int iterations
        )
        {
            long before = GC.GetAllocatedBytesForCurrentThread();
            for (int i = 0; i < iterations; i++)
            {
                script.Call(callback, first, second, third);
            }

            return GC.GetAllocatedBytesForCurrentThread() - before;
        }

        private static long MeasureDirectFiveArgumentCallbackViewAllocations(
            Script script,
            DynValue callback,
            DynValue self,
            DynValue first,
            DynValue second,
            DynValue third,
            DynValue fourth,
            int iterations
        )
        {
            long before = GC.GetAllocatedBytesForCurrentThread();
            for (int i = 0; i < iterations; i++)
            {
                DynValue result = script.Call(callback, self, first, second, third, fourth);
                if (result.Type != DataType.Nil)
                {
                    throw new InvalidOperationException(
                        "Direct callback-view allocation probe returned an unexpected value."
                    );
                }
            }

            return GC.GetAllocatedBytesForCurrentThread() - before;
        }

        private static long MeasureFixedFourArgumentCallbackViewMetamethodAllocations(
            Script script,
            DynValue callable,
            DynValue first,
            DynValue second,
            DynValue third,
            DynValue fourth,
            int iterations
        )
        {
            long before = GC.GetAllocatedBytesForCurrentThread();
            for (int i = 0; i < iterations; i++)
            {
                DynValue result = script.Call(callable, first, second, third, fourth);
                if (result.Type != DataType.Nil)
                {
                    throw new InvalidOperationException(
                        "Metamethod allocation probe returned an unexpected value."
                    );
                }
            }

            return GC.GetAllocatedBytesForCurrentThread() - before;
        }

        private static long MeasureFixedFiveArgumentCallbackViewMetamethodAllocations(
            Script script,
            DynValue callable,
            DynValue first,
            DynValue second,
            DynValue third,
            DynValue fourth,
            DynValue fifth,
            int iterations
        )
        {
            long before = GC.GetAllocatedBytesForCurrentThread();
            for (int i = 0; i < iterations; i++)
            {
                DynValue result = script.Call(callable, first, second, third, fourth, fifth);
                if (result.Type != DataType.Nil)
                {
                    throw new InvalidOperationException(
                        "Five-argument metamethod allocation probe returned an unexpected value."
                    );
                }
            }

            return GC.GetAllocatedBytesForCurrentThread() - before;
        }

        private static long MeasureFixedThreeArgumentCallbackViewChainedMetamethodAllocations(
            Script script,
            DynValue callable,
            DynValue first,
            DynValue second,
            DynValue third,
            int iterations
        )
        {
            long before = GC.GetAllocatedBytesForCurrentThread();
            for (int i = 0; i < iterations; i++)
            {
                DynValue result = script.Call(callable, first, second, third);
                if (result.Type != DataType.Nil)
                {
                    throw new InvalidOperationException(
                        "Chained metamethod allocation probe returned an unexpected value."
                    );
                }
            }

            return GC.GetAllocatedBytesForCurrentThread() - before;
        }

        private static long MeasureFixedFourArgumentCallbackViewChainedMetamethodAllocations(
            Script script,
            DynValue callable,
            DynValue first,
            DynValue second,
            DynValue third,
            DynValue fourth,
            int iterations
        )
        {
            long before = GC.GetAllocatedBytesForCurrentThread();
            for (int i = 0; i < iterations; i++)
            {
                DynValue result = script.Call(callable, first, second, third, fourth);
                if (result.Type != DataType.Nil)
                {
                    throw new InvalidOperationException(
                        "Four-argument chained metamethod allocation probe returned an unexpected value."
                    );
                }
            }

            return GC.GetAllocatedBytesForCurrentThread() - before;
        }

        private static long MeasureFixedFiveArgumentCallbackViewChainedMetamethodAllocations(
            Script script,
            DynValue callable,
            DynValue first,
            DynValue second,
            DynValue third,
            DynValue fourth,
            DynValue fifth,
            int iterations
        )
        {
            long before = GC.GetAllocatedBytesForCurrentThread();
            for (int i = 0; i < iterations; i++)
            {
                DynValue result = script.Call(callable, first, second, third, fourth, fifth);
                if (result.Type != DataType.Nil)
                {
                    throw new InvalidOperationException(
                        "Five-argument chained metamethod allocation probe returned an unexpected value."
                    );
                }
            }

            return GC.GetAllocatedBytesForCurrentThread() - before;
        }

        private static long MeasureSpanCallbackViewMetamethodAllocations(
            Script script,
            DynValue callable,
            DynValue[] args,
            int iterations
        )
        {
            long before = GC.GetAllocatedBytesForCurrentThread();
            for (int i = 0; i < iterations; i++)
            {
                DynValue result = script.Call(callable, args.AsSpan());
                if (result.Type != DataType.Nil)
                {
                    throw new InvalidOperationException(
                        "Span metamethod allocation probe returned an unexpected value."
                    );
                }
            }

            return GC.GetAllocatedBytesForCurrentThread() - before;
        }

        private static long MeasureArrayCallbackViewMetamethodAllocations(
            Script script,
            DynValue callable,
            DynValue[] args,
            int iterations
        )
        {
            long before = GC.GetAllocatedBytesForCurrentThread();
            for (int i = 0; i < iterations; i++)
            {
                DynValue result = script.Call(callable, args);
                if (result.Type != DataType.Nil)
                {
                    throw new InvalidOperationException(
                        "Array metamethod allocation probe returned an unexpected value."
                    );
                }
            }

            return GC.GetAllocatedBytesForCurrentThread() - before;
        }

        private static DynValue CallWithSpan(Script script, DynValue function, DynValue[] args)
        {
            return script.Call(function, args.AsSpan());
        }

        private static DynValue CallWithParamsArray(
            Script script,
            DynValue function,
            DynValue[] args
        )
        {
            return script.Call(function, args);
        }

        private sealed class UnregisteredHostObject { }

        private sealed class StubScriptLoader : ScriptLoaderBase
        {
            internal int ResolveCalls { get; private set; }
            internal int LoadCalls { get; private set; }
            internal bool ResolveReturnsNull { get; set; }
            internal string ModuleSource { get; set; } = "return function() end";
            internal Table LastGlobalContext { get; private set; }

            public override object LoadFile(string file, Table globalContext)
            {
                LoadCalls++;
                return ModuleSource;
            }

            public override bool ScriptFileExists(string name)
            {
                return true;
            }

            public override string ResolveModuleName(string modname, Table globalContext)
            {
                LastGlobalContext = globalContext;
                ResolveCalls++;
                return ResolveReturnsNull ? null : modname;
            }

            public override ModuleResolutionResult TryResolveModuleName(
                string modname,
                Table globalContext
            )
            {
                string resolved = ResolveModuleName(modname, globalContext);
                return resolved != null
                    ? ModuleResolutionResult.Success(resolved, Array.Empty<string>())
                    : ModuleResolutionResult.NotFound(Array.Empty<string>());
            }
        }
    }
}
