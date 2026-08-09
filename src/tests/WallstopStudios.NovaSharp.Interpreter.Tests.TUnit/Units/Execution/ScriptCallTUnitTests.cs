namespace WallstopStudios.NovaSharp.Interpreter.Tests.TUnit.Units.Execution
{
    using System;
    using System.Collections.Generic;
    using System.Threading.Tasks;
    using global::NovaSharp;
    using global::TUnit.Assertions;
    using WallstopStudios.NovaSharp.Interpreter;
    using WallstopStudios.NovaSharp.Interpreter.Compatibility;
    using WallstopStudios.NovaSharp.Interpreter.DataTypes;
    using WallstopStudios.NovaSharp.Interpreter.Errors;
    using WallstopStudios.NovaSharp.Interpreter.Execution;
    using WallstopStudios.NovaSharp.Interpreter.Loaders;
    using WallstopStudios.NovaSharp.Interpreter.Modules;
    using WallstopStudios.NovaSharp.Tests.TestInfrastructure.Scopes;
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
            LuaValue function = script.Globals.Get("noop");

            ArgumentNullException exception = Assert.Throws<ArgumentNullException>(() =>
                script.Call(function, (LuaValue[])null)
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
            LuaValue function = script.Globals.Get("noop");

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

            ArgumentException exception = Assert.Throws<ArgumentException>(() =>
                script.CallValues(LuaValue.Nil)
            );

            await Assert.That(exception.Message).Contains("not a function").ConfigureAwait(false);
        }

        [global::TUnit.Core.Test]
        [AllLuaVersions]
        public async Task DynValueCallInvokesZeroArgumentLuaFunction(
            LuaCompatibilityVersion version
        )
        {
            Script script = new(version, CoreModulePresets.Complete);
            LuaValue function = script.DoString("return function() return 42 end");

            LuaValue result = script.CallValues(function);

            await Assert.That(result.Number).IsEqualTo(42d).ConfigureAwait(false);
        }

        [global::TUnit.Core.Test]
        [AllLuaVersions]
        public async Task DynValueCallPreservesDebugFrameFunctionIdentity(
            LuaCompatibilityVersion version
        )
        {
            Script script = new(version, CoreModulePresets.Complete);
            LuaValue function = script.DoString(
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

            LuaValue result = script.CallValues(function);

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
            LuaValue function = script.DoString(
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

            LuaValue spanResult = CallWithSpan(script, function, Array.Empty<LuaValue>());
            LuaValue paramsResult = CallWithParamsArray(script, function, Array.Empty<LuaValue>());

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
            script.Globals.Set("marker", LuaValue.FromNumber(5));
            LuaValue function = script.DoString(
                """
                return function()
                    local before = getfenv(1).marker
                    setfenv(1, { marker = 99, getfenv = getfenv, setfenv = setfenv })
                    return before * 100 + getfenv(1).marker
                end
                """,
                codeFriendlyName: "call_setfenv.lua"
            );

            LuaValue result = script.CallValues(function);

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

            LuaValue callable = script.Globals.Get("callable");
            LuaValue result = script.CallValues(callable, LuaValue.NewNumber(21));

            await Assert.That(result.Number).IsEqualTo(42d).ConfigureAwait(false);
        }

        [global::TUnit.Core.Test]
        [AllLuaVersions]
        public async Task DynValueArgumentsDoNotAliasHostValues(LuaCompatibilityVersion version)
        {
            Script script = new(version, CoreModulePresets.Complete);
            LuaValue function = script.DoString(
                """
                return function(a, ...)
                    local b, c = ...
                    a = a + 10
                    b = b + 20
                    c = c + 30
                    return a, b, c, select("#", ...)
                end
                """
            );
            LuaValue first = LuaValue.FromInteger(1);
            LuaValue second = LuaValue.NewInteger(2);
            LuaValue third = LuaValue.FromInteger(3);

            LuaValue result = script.CallValues(function, first, second, third);

            await Assert.That(result.Tuple[0].Number).IsEqualTo(11d).ConfigureAwait(false);
            await Assert.That(result.Tuple[1].Number).IsEqualTo(22d).ConfigureAwait(false);
            await Assert.That(result.Tuple[2].Number).IsEqualTo(33d).ConfigureAwait(false);
            await Assert.That(result.Tuple[3].Number).IsEqualTo(2d).ConfigureAwait(false);
            await Assert.That(first.Number).IsEqualTo(1d).ConfigureAwait(false);
            await Assert.That(second.Number).IsEqualTo(2d).ConfigureAwait(false);
            await Assert.That(third.Number).IsEqualTo(3d).ConfigureAwait(false);
        }

        [global::TUnit.Core.Test]
        [AllLuaVersions]
        public async Task VarargCapturePreservesScalarsAndKeepsTableReferences(
            LuaCompatibilityVersion version
        )
        {
            Script script = new(version, CoreModulePresets.Complete);
            LuaValue returnVarargs = script.DoString("return function(...) return ... end");
            LuaValue captureTable = script.DoString("return function(...) return {...} end");
            LuaValue first = LuaValue.NewNumber(1);
            LuaValue second = LuaValue.NewString("two");

            LuaValue returned = script.CallValues(returnVarargs, first, second);

            await Assert.That(returned.Tuple[0].Number).IsEqualTo(1d).ConfigureAwait(false);
            await Assert.That(returned.Tuple[1].String).IsEqualTo("two").ConfigureAwait(false);

            LuaValue third = LuaValue.NewNumber(3);
            LuaValue fourth = LuaValue.NewString("four");
            LuaValue captured = script.CallValues(captureTable, third, fourth);

            await Assert.That(captured.Table.Get(1).Number).IsEqualTo(3d).ConfigureAwait(false);
            await Assert.That(captured.Table.Get(2).String).IsEqualTo("four").ConfigureAwait(false);

            Table table = new(script);
            table.Set("field", LuaValue.NewNumber(1));
            LuaValue tableArg = LuaValue.NewTable(table);
            LuaValue tableCapture = script.CallValues(
                captureTable,
                tableArg,
                LuaValue.NewNumber(0)
            );
            LuaValue capturedTable = tableCapture.Table.Get(1);
            capturedTable.Table.Set("field", LuaValue.NewNumber(2));

            await Assert.That(capturedTable.Table).IsSameReferenceAs(table).ConfigureAwait(false);
            await Assert.That(table.Get("field").Number).IsEqualTo(2d).ConfigureAwait(false);
        }

        [global::TUnit.Core.Test]
        [LuaVersionsFrom(LuaCompatibilityVersion.Lua52)]
        public async Task TablePackVarargsPreservesScalars(LuaCompatibilityVersion version)
        {
            Script script = new(version, CoreModulePresets.Complete);
            LuaValue pack = script.DoString("return function(...) return table.pack(...) end");
            LuaValue first = LuaValue.NewNumber(1);
            LuaValue second = LuaValue.NewString("two");

            LuaValue packed = script.CallValues(pack, first, second);

            await Assert.That(packed.Table.Get(1).Number).IsEqualTo(1d).ConfigureAwait(false);
            await Assert.That(packed.Table.Get(2).String).IsEqualTo("two").ConfigureAwait(false);
            await Assert.That(packed.Table.Get("n").Number).IsEqualTo(2d).ConfigureAwait(false);
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

            LuaValue callable = script.Globals.Get("callable");
            LuaValue result = script.CallValues(
                callable,
                LuaValue.NewNumber(10),
                LuaValue.NewNumber(20),
                LuaValue.NewNumber(30),
                LuaValue.NewNumber(40)
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
            LuaValue function = script.DoString(
                "return function(a, b, c, d, e) return a + b + c + d + e end"
            );

            LuaValue result = script.CallValues(
                function,
                LuaValue.NewNumber(1),
                LuaValue.NewNumber(2),
                LuaValue.NewNumber(3),
                LuaValue.NewNumber(4),
                LuaValue.NewNumber(5)
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

            LuaValue callable = script.Globals.Get("callable");
            LuaValue result = script.CallValues(
                callable,
                LuaValue.NewNumber(1),
                LuaValue.NewNumber(2),
                LuaValue.NewNumber(3),
                LuaValue.NewNumber(4),
                LuaValue.NewNumber(5)
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
            LuaValue callback = LuaValue.NewCallback((_, _) => LuaValue.NewString("clr"));

            LuaValue result = script.CallValues(callback);

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
            LuaValue callback = LuaValue.NewCallback(
                (_, args) =>
                    LuaValue.NewNumber(
                        args.Count
                            + args[0].Number
                            + args[1].Number
                            + args[2].Number
                            + args[3].Number
                    )
            );

            LuaValue result = script.CallValues(
                callback,
                LuaValue.NewNumber(10),
                LuaValue.NewNumber(20),
                LuaValue.NewNumber(30),
                LuaValue.NewNumber(40)
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
            LuaValue callback = LuaValue.NewCallback(
                (_, args) =>
                    LuaValue.NewNumber(
                        args.Count
                            + args[0].Number
                            + args[1].Number
                            + args[2].Number
                            + args[3].Number
                            + args[4].Number
                    )
            );

            LuaValue result = script.CallValues(
                callback,
                LuaValue.NewNumber(10),
                LuaValue.NewNumber(20),
                LuaValue.NewNumber(30),
                LuaValue.NewNumber(40),
                LuaValue.NewNumber(50)
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
            LuaValue callback = LuaValue.NewCallbackView(
                (_, args) =>
                    LuaValue.NewNumber(
                        args.Count
                            + args[0].Number
                            + args[1].Number
                            + args[2].Number
                            + args[3].Number
                    )
            );

            LuaValue result = script.CallValues(
                callback,
                LuaValue.NewNumber(10),
                LuaValue.NewNumber(20),
                LuaValue.NewNumber(30),
                LuaValue.NewNumber(40)
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
            LuaValue callback = LuaValue.NewCallbackView(
                (_, args) =>
                {
                    spanAvailable = args.TryGetSpan(out ReadOnlySpan<LuaValue> span);
                    spanLength = span.Length;
                    return LuaValue.NewNumber(
                        args.Count
                            + args[0].Number
                            + args[1].Number
                            + args[2].Number
                            + args[3].Number
                            + args[4].Number
                    );
                }
            );

            LuaValue result = script.CallValues(
                callback,
                LuaValue.NewNumber(10),
                LuaValue.NewNumber(20),
                LuaValue.NewNumber(30),
                LuaValue.NewNumber(40),
                LuaValue.NewNumber(50)
            );

            await Assert.That(result.Number).IsEqualTo(155d).ConfigureAwait(false);
            await Assert.That(spanAvailable).IsFalse().ConfigureAwait(false);
            await Assert.That(spanLength).IsEqualTo(0).ConfigureAwait(false);
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
        public async Task FixedDynValueCallExecutesNoContextCallbackView(int arity)
        {
            Script script = new(CoreModulePresets.Complete);
            LuaValue callback = LuaValue.NewCallbackView(
                (CallbackArgumentsView args) => SummarizeArguments(args)
            );

            LuaValue result = CallLegacyCallbackWithSequentialArguments(script, callback, arity);

            await AssertArgumentSummary(
                    result,
                    count: arity,
                    nilCount: 0d,
                    sum: arity * (arity + 1) / 2d
                )
                .ConfigureAwait(false);
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
            LuaValue callback = LuaValue.NewCallbackView(
                (_, args) =>
                    LuaValue.NewNumber(
                        args.Count + args[0].Number + args[1].Number + args[2].Number
                    )
            );

            LuaValue result = script.CallValues(
                callback,
                LuaValue.NewNumber(10),
                LuaValue.NewTuple(LuaValue.NewNumber(20), LuaValue.NewNumber(30))
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
            LuaValue callback = LuaValue.NewCallbackView(
                (_, args) => LuaValue.NewNumber(args.Count + args[0].Number)
            );

            LuaValue result = script.CallValues(callback, LuaValue.NewNumber(10), LuaValue.Void);

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

            script.Globals["callback"] = LuaValue.NewCallbackView(
                (_, args) =>
                {
                    spanAvailable = args.TryGetSpan(out ReadOnlySpan<LuaValue> span);
                    spanLength = span.Length;
                    if (spanAvailable)
                    {
                        first = span[0].Number;
                        third = span[2].Number;
                    }

                    return LuaValue.NewNumber(args.Count);
                }
            );

            LuaValue result = script.DoString("return callback(10, 20, 30)");

            await Assert.That(result.Number).IsEqualTo(3d).ConfigureAwait(false);
            await Assert.That(spanAvailable).IsTrue().ConfigureAwait(false);
            await Assert.That(spanLength).IsEqualTo(3).ConfigureAwait(false);
            await Assert.That(first).IsEqualTo(10d).ConfigureAwait(false);
            await Assert.That(third).IsEqualTo(30d).ConfigureAwait(false);
        }

        [global::TUnit.Core.Test]
        [AllLuaVersions]
        public async Task LuaCallToNoContextCallbackViewExposesContiguousSpan(
            LuaCompatibilityVersion version
        )
        {
            Script script = new(version, CoreModulePresets.Complete);
            bool spanAvailable = false;
            int spanLength = -1;
            double first = -1d;
            double third = -1d;

            script.Globals["callback"] = LuaValue.NewCallbackView(
                (CallbackArgumentsView args) =>
                {
                    spanAvailable = args.TryGetSpan(out ReadOnlySpan<LuaValue> span);
                    spanLength = span.Length;
                    if (spanAvailable)
                    {
                        first = span[0].Number;
                        third = span[2].Number;
                    }

                    return LuaValue.NewNumber(args.Count);
                }
            );

            LuaValue result = script.DoString("return callback(10, 20, 30)");

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

            script.Globals["callback"] = LuaValue.NewCallbackView(
                (_, args) =>
                {
                    if (args.Count == 0)
                    {
                        zeroCount = args.Count;
                        return LuaValue.NewNumber(0);
                    }

                    manyCount = args.Count;
                    manySpanAvailable = args.TryGetSpan(out ReadOnlySpan<LuaValue> span);
                    manySpanLength = span.Length;
                    return LuaValue.NewNumber(args[4].Number + args.Count);
                }
            );

            LuaValue result = script.DoString("callback(); return callback(1, 2, 3, 4, 5)");

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

            script.Globals["callback"] = LuaValue.NewCallbackView(
                (_, args) =>
                {
                    spanAvailable = args.TryGetSpan(out ReadOnlySpan<LuaValue> ignored);
                    spanAvailable = spanAvailable && ignored.Length >= 0;
                    return LuaValue.NewNumber(
                        args.Count + args[0].Number + args[1].Number + args[2].Number
                    );
                }
            );

            LuaValue result = script.DoString(
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

            script.Globals["callback"] = LuaValue.NewCallbackView(
                (_, args) =>
                {
                    spanAvailable = args.TryGetSpan(out ReadOnlySpan<LuaValue> span);
                    spanLength = span.Length;
                    return LuaValue.NewNumber(args.Count + args[0].Number);
                }
            );

            LuaValue result = script.DoString(
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

            script.Globals["callback"] = LuaValue.NewCallbackView(
                (_, args) =>
                {
                    spanAvailable = args.TryGetSpan(out ReadOnlySpan<LuaValue> span);
                    spanLength = span.Length;
                    return LuaValue.NewNumber(args.Count + args[0].Number + args[1].Number);
                }
            );

            LuaValue result = script.DoString(
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

            script.Globals["empty"] = LuaValue.NewCallbackView((_, _) => LuaValue.EmptyTuple);
            script.Globals["callback"] = LuaValue.NewCallbackView(
                (_, args) =>
                {
                    spanAvailable = args.TryGetSpan(out ReadOnlySpan<LuaValue> span);
                    spanAvailable = spanAvailable && span.Length >= 0;
                    return LuaValue.NewNumber(args.Count + args[0].Number);
                }
            );

            LuaValue result = script.DoString("return callback(10, empty())");

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

            script.Globals["callback"] = LuaValue.NewCallbackView(
                (_, args) => LuaValue.NewNumber(args.Count + args[0].Number + args[1].Number)
            );

            LuaValue result = script.DoString(
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

            script.Globals["callback"] = LuaValue.NewCallback(
                (_, args) =>
                {
                    spanAvailable = args.TryGetSpan(out ReadOnlySpan<LuaValue> span);
                    spanLength = span.Length;
                    first = args[0].Number;
                    seventh = args[6].Number;
                    return LuaValue.NewNumber(args.Count + first + seventh);
                }
            );

            LuaValue result = script.DoString("return callback(1, 2, 3, 4, 5, 6, 7)");

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
            script.Globals["callback"] = LuaValue.NewCallback(
                (_, args) => SummarizeArguments(args)
            );

            LuaValue tupleResult = script.DoString(
                @"
                local function values()
                    return 20, 30
                end

                return callback(10, values())
            "
            );

            await AssertArgumentSummary(tupleResult, count: 3d, nilCount: 0d, sum: 60d)
                .ConfigureAwait(false);

            LuaValue noReturnResult = script.DoString(
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
            script.Globals["callback"] = LuaValue.NewCallback(
                (_, args) => SummarizeArguments(args)
            );
            script.Globals["voidTuple"] = LuaValue.NewCallback(
                (_, _) => LuaValue.NewTuple(LuaValue.NewNumber(20), LuaValue.Void)
            );
            script.Globals["nestedTuple"] = LuaValue.NewCallback(
                (_, _) =>
                    LuaValue.NewTuple(
                        LuaValue.NewNumber(20),
                        LuaValue.NewTuple(LuaValue.NewNumber(30), LuaValue.NewNumber(40))
                    )
            );

            LuaValue voidResult = script.DoString("return callback(10, voidTuple())");
            await AssertArgumentSummary(voidResult, count: 2d, nilCount: 0d, sum: 30d)
                .ConfigureAwait(false);

            LuaValue nestedResult = script.DoString("return callback(10, nestedTuple())");
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
            LuaValue callback = LuaValue.NewCallback(
                (_, args) =>
                {
                    bool success = args.TryGetSpan(out ReadOnlySpan<LuaValue> span);
                    LuaValue[] copied = new LuaValue[args.Count];
                    int copiedCount = args.CopyTo(copied);
                    double sum = 0d;
                    for (int i = 0; i < copiedCount; i++)
                    {
                        sum += copied[i].Number;
                    }

                    return LuaValue.NewTuple(
                        LuaValue.NewNumber(args.Count),
                        LuaValue.NewBoolean(success),
                        LuaValue.NewNumber(span.Length),
                        LuaValue.NewNumber(copiedCount),
                        LuaValue.NewNumber(sum)
                    );
                }
            );

            LuaValue result = CallLegacyCallbackWithSequentialArguments(script, callback, arity);

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
            LuaValue inspect = LuaValue.NewCallback(
                (_, args) =>
                {
                    double sum = 0d;
                    for (int i = 0; i < args.Count; i++)
                    {
                        sum += args[i].Number;
                    }

                    return LuaValue.NewTuple(
                        LuaValue.NewNumber(args.Count),
                        LuaValue.NewNumber(sum)
                    );
                }
            );

            LuaValue result = CallLegacyCallbackWithSequentialArguments(script, inspect, arity);

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
            LuaValue inspect = LuaValue.NewCallback((_, args) => SummarizeArguments(args));

            LuaValue result = arity switch
            {
                1 => script.CallValues(inspect, LuaValue.Nil),
                2 => script.CallValues(inspect, LuaValue.NewNumber(1), LuaValue.Void),
                3 => script.CallValues(
                    inspect,
                    LuaValue.NewNumber(1),
                    LuaValue.NewTuple(LuaValue.NewNumber(2), LuaValue.NewNumber(20)),
                    LuaValue.NewTuple(LuaValue.NewNumber(3), LuaValue.Nil)
                ),
                4 => script.CallValues(
                    inspect,
                    LuaValue.Nil,
                    LuaValue.NewNumber(2),
                    LuaValue.NewNumber(3),
                    LuaValue.NewTuple(LuaValue.NewNumber(4), LuaValue.Nil)
                ),
                5 => script.CallValues(
                    inspect,
                    LuaValue.NewNumber(1),
                    LuaValue.Nil,
                    LuaValue.NewTuple(LuaValue.NewNumber(2), LuaValue.NewNumber(20)),
                    LuaValue.NewNumber(3),
                    LuaValue.NewTuple(LuaValue.NewNumber(4), LuaValue.Nil)
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
            LuaValue inspect = LuaValue.NewCallback(
                (_, args) =>
                {
                    bool spanAvailable = args.TryGetSpan(out ReadOnlySpan<LuaValue> span);
                    return LuaValue.NewTuple(
                        LuaValue.NewBoolean(spanAvailable),
                        LuaValue.NewNumber(span.Length),
                        LuaValue.NewNumber(args.Count),
                        args[0]
                    );
                }
            );

            LuaValue nonFinalTuple = script.CallValues(
                inspect,
                LuaValue.NewTuple(LuaValue.NewNumber(1), LuaValue.NewNumber(10)),
                LuaValue.NewNumber(2)
            );
            LuaValue nonFinalVoid = script.CallValues(
                inspect,
                LuaValue.Void,
                LuaValue.NewNumber(2)
            );

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
        }

        [global::TUnit.Core.Test]
        public async Task FixedDynValueCallToLegacyClrFunctionPreservesTrailingExpansionEdges()
        {
            Script script = new(CoreModulePresets.Complete);
            LuaValue inspect = LuaValue.NewCallback((_, args) => SummarizeArguments(args));

            LuaValue oneVoid = script.CallValues(inspect, LuaValue.Void);
            LuaValue oneTuple = script.CallValues(
                inspect,
                LuaValue.NewTuple(LuaValue.NewNumber(2), LuaValue.Nil)
            );
            LuaValue twoTuple = script.CallValues(
                inspect,
                LuaValue.NewNumber(1),
                LuaValue.NewTuple(LuaValue.NewNumber(2), LuaValue.Nil)
            );
            LuaValue threeVoid = script.CallValues(
                inspect,
                LuaValue.NewNumber(1),
                LuaValue.NewNumber(2),
                LuaValue.Void
            );
            LuaValue fourVoid = script.CallValues(
                inspect,
                LuaValue.NewNumber(1),
                LuaValue.NewNumber(2),
                LuaValue.NewNumber(3),
                LuaValue.Void
            );
            LuaValue oneEmptyTuple = script.CallValues(inspect, LuaValue.EmptyTuple);
            LuaValue twoEmptyTuple = script.CallValues(
                inspect,
                LuaValue.NewNumber(1),
                LuaValue.EmptyTuple
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
                LuaValue.NewCallback(
                    (_, args) =>
                        LuaValue.NewTuple(
                            LuaValue.NewBoolean(args.Count == 2),
                            LuaValue.NewBoolean(args.IsMethodCall),
                            LuaValue.NewBoolean(ReferenceEquals(args[0].Table, callable)),
                            args[1]
                        )
                )
            );
            callable.MetaTable = meta;

            LuaValue result = script.CallValues(
                LuaValue.NewTable(callable),
                LuaValue.NewNumber(42)
            );

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

            targetMeta.Set("__call", LuaValue.NewTable(proxy));
            proxyMeta.Set(
                "__call",
                LuaValue.NewCallback((_, _) => LuaValue.NewString("unexpected"))
            );
            target.MetaTable = targetMeta;
            proxy.MetaTable = proxyMeta;

            ArgumentException exception = Assert.Throws<ArgumentException>(() =>
                script.CallValues(LuaValue.NewTable(target))
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
            LuaValue target = CreateTableValuedCallChain(
                script,
                (_, _, _) => LuaValue.NewString("unexpected")
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

            targetMeta.Set("__call", LuaValue.NewTable(proxy));
            proxyMeta.Set(
                "__call",
                LuaValue.NewCallback(
                    (_, args) =>
                        LuaValue.NewTuple(
                            LuaValue.NewBoolean(args.Count == 2),
                            LuaValue.NewBoolean(ReferenceEquals(args[0].Table, proxy)),
                            LuaValue.NewBoolean(ReferenceEquals(args[1].Table, target)),
                            LuaValue.NewBoolean(args.IsMethodCall)
                        )
                )
            );
            target.MetaTable = targetMeta;
            proxy.MetaTable = proxyMeta;

            LuaValue result = script.CallValues(LuaValue.NewTable(target));

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
            LuaValue target = CreateTableValuedCallChain(
                script,
                (targetTable, proxyTable, args) =>
                    SummarizeChainedCallArguments(targetTable, proxyTable, args)
            );

            LuaValue result = CallLegacyCallbackWithSequentialArguments(script, target, arity);

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
            LuaValue target = CreateTableValuedCallChain(
                script,
                (targetTable, proxyTable, args) =>
                    SummarizeChainedCallArguments(targetTable, proxyTable, args)
            );

            LuaValue result = CallLegacyCallbackWithSequentialArguments(script, target, arity);

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
                meta.Set("__call", LuaValue.NewTable(next));
                current.MetaTable = meta;
                current = next;
            }

            ScriptRuntimeException exception = Assert.Throws<ScriptRuntimeException>(() =>
                script.CallValues(LuaValue.NewTable(root))
            );

            await Assert.That(exception.Message).Contains("loop").ConfigureAwait(false);
        }

        [global::TUnit.Core.Test]
        public async Task FixedDynValueCallToLegacyClrFunctionAvoidsArgumentArrayAllocation()
        {
            const int iterations = 1024;
            Script script = new(CoreModulePresets.Complete);
            LuaValue first = LuaValue.NewNumber(10);
            LuaValue second = LuaValue.NewNumber(20);
            LuaValue third = LuaValue.NewNumber(30);
            LuaValue noArgCallback = LuaValue.NewCallback(
                (_, args) =>
                {
                    if (args.Count != 0)
                    {
                        throw new InvalidOperationException("Unexpected no-arg callback arity.");
                    }

                    return LuaValue.Nil;
                }
            );
            LuaValue fixedCallback = LuaValue.NewCallback(
                (_, args) =>
                {
                    if (args.Count != 3)
                    {
                        throw new InvalidOperationException("Unexpected fixed callback arity.");
                    }

                    return args[2];
                }
            );
            LuaValue spanProbeCallback = LuaValue.NewCallback(
                (_, args) =>
                {
                    bool spanAvailable = args.TryGetSpan(out ReadOnlySpan<LuaValue> span);
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
        public async Task FixedDynValueCallToNoContextCallbackViewAvoidsContextAllocation()
        {
            const int iterations = 1024;
            Script script = new(CoreModulePresets.Complete);
            LuaValue first = LuaValue.NewNumber(10);
            LuaValue second = LuaValue.NewNumber(20);
            LuaValue third = LuaValue.NewNumber(30);
            LuaValue contextCallback = LuaValue.NewCallbackView((_, args) => args[2]);
            LuaValue noContextCallback = LuaValue.NewCallbackView(
                (CallbackArgumentsView args) => args[2]
            );

            MeasureFixedThreeArgumentCallbackViewAllocations(
                script,
                contextCallback,
                first,
                second,
                third,
                iterations: 8
            );
            MeasureFixedThreeArgumentCallbackViewAllocations(
                script,
                noContextCallback,
                first,
                second,
                third,
                iterations: 8
            );

            GC.Collect();
            GC.WaitForPendingFinalizers();
            GC.Collect();

            long contextAllocated = MeasureFixedThreeArgumentCallbackViewAllocations(
                script,
                contextCallback,
                first,
                second,
                third,
                iterations
            );

            GC.Collect();
            GC.WaitForPendingFinalizers();
            GC.Collect();

            long noContextAllocated = MeasureFixedThreeArgumentCallbackViewAllocations(
                script,
                noContextCallback,
                first,
                second,
                third,
                iterations
            );

            await Assert
                .That(noContextAllocated)
                .IsLessThan(contextAllocated)
                .Because(
                    $"Contextful callback-view calls allocated {contextAllocated} bytes; no-context callback-view calls allocated {noContextAllocated} bytes."
                )
                .ConfigureAwait(false);
        }

        [global::TUnit.Core.Test]
        public async Task FixedDynValueCallToCallbackViewMetamethodAvoidsArgumentArrayAllocation()
        {
            const int iterations = 1024;
            Script script = new(CoreModulePresets.Complete);
            Table callable = new(script);
            Table meta = new(script);
            LuaValue callableValue = LuaValue.NewTable(callable);
            LuaValue first = LuaValue.NewNumber(1);
            LuaValue second = LuaValue.NewNumber(2);
            LuaValue third = LuaValue.NewNumber(3);
            LuaValue fourth = LuaValue.NewNumber(4);
            LuaValue callback = LuaValue.NewCallbackView(
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

                    return LuaValue.Nil;
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
            LuaValue callableValue = LuaValue.NewTable(callable);
            LuaValue first = LuaValue.NewNumber(1);
            LuaValue second = LuaValue.NewNumber(2);
            LuaValue third = LuaValue.NewNumber(3);
            LuaValue fourth = LuaValue.NewNumber(4);
            LuaValue fifth = LuaValue.NewNumber(5);
            LuaValue fourArgumentCallback = LuaValue.NewCallbackView(
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

                    return LuaValue.Nil;
                }
            );
            LuaValue fiveArgumentCallback = LuaValue.NewCallbackView(
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

                    return LuaValue.Nil;
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
            LuaValue targetValue = LuaValue.NewTable(target);
            LuaValue proxyValue = LuaValue.NewTable(proxy);
            LuaValue first = LuaValue.NewNumber(1);
            LuaValue second = LuaValue.NewNumber(2);
            LuaValue third = LuaValue.NewNumber(3);
            LuaValue callback = LuaValue.NewCallbackView(
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

                    return LuaValue.Nil;
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
            LuaValue targetValue = LuaValue.NewTable(target);
            LuaValue proxyValue = LuaValue.NewTable(proxy);
            LuaValue first = LuaValue.NewNumber(1);
            LuaValue second = LuaValue.NewNumber(2);
            LuaValue third = LuaValue.NewNumber(3);
            LuaValue fourth = LuaValue.NewNumber(4);
            LuaValue fifth = LuaValue.NewNumber(5);
            LuaValue callback = LuaValue.NewCallbackView(
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

                    return LuaValue.Nil;
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
        public async Task FixedSixDynValueCallToChainedCallbackViewMetamethodAvoidsFallbackArgumentArrayAllocation()
        {
            const int iterations = 1024;
            Script script = new(CoreModulePresets.Complete);
            Table target = new(script);
            Table proxy = new(script);
            Table targetMeta = new(script);
            Table proxyMeta = new(script);
            LuaValue targetValue = LuaValue.NewTable(target);
            LuaValue proxyValue = LuaValue.NewTable(proxy);
            LuaValue first = LuaValue.NewNumber(1);
            LuaValue second = LuaValue.NewNumber(2);
            LuaValue third = LuaValue.NewNumber(3);
            LuaValue fourth = LuaValue.NewNumber(4);
            LuaValue fifth = LuaValue.NewNumber(5);
            LuaValue sixth = LuaValue.NewNumber(6);
            int expectedCount = 0;
            LuaValue callback = LuaValue.NewCallbackView(
                (_, args) =>
                {
                    if (expectedCount == 8)
                    {
                        if (
                            !args.TryGetSpan(out ReadOnlySpan<LuaValue> span)
                            || span.Length != expectedCount
                            || !ReferenceEquals(span[0].Table, proxy)
                            || !ReferenceEquals(span[1].Table, target)
                        )
                        {
                            throw new InvalidOperationException(
                                "Six-argument chained metamethod allocation probe received unexpected span/self arguments."
                            );
                        }

                        for (int i = 2; i < span.Length; i++)
                        {
                            if (span[i].Number != i - 1d)
                            {
                                throw new InvalidOperationException(
                                    "Six-argument chained metamethod allocation probe received unexpected user arguments."
                                );
                            }
                        }
                    }
                    else if (
                        args.Count != expectedCount
                        || !ReferenceEquals(args[0].Table, proxy)
                        || !ReferenceEquals(args[1].Table, target)
                    )
                    {
                        throw new InvalidOperationException(
                            "Six-argument chained metamethod allocation probe received unexpected fixed/self arguments."
                        );
                    }
                    else
                    {
                        for (int i = 2; i < args.Count; i++)
                        {
                            if (args[i].Number != i - 1d)
                            {
                                throw new InvalidOperationException(
                                    "Six-argument chained metamethod allocation probe received unexpected user arguments."
                                );
                            }
                        }
                    }

                    return LuaValue.Nil;
                }
            );
            targetMeta.Set("__call", proxyValue);
            proxyMeta.Set("__call", callback);
            target.MetaTable = targetMeta;
            proxy.MetaTable = proxyMeta;

            expectedCount = 7;
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
            expectedCount = 8;
            MeasureFixedSixArgumentCallbackViewChainedMetamethodAllocations(
                script,
                targetValue,
                first,
                second,
                third,
                fourth,
                fifth,
                sixth,
                iterations: 8
            );

            GC.Collect();
            GC.WaitForPendingFinalizers();
            GC.Collect();

            expectedCount = 7;
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

            GC.Collect();
            GC.WaitForPendingFinalizers();
            GC.Collect();

            expectedCount = 8;
            long sixArgumentAllocated =
                MeasureFixedSixArgumentCallbackViewChainedMetamethodAllocations(
                    script,
                    targetValue,
                    first,
                    second,
                    third,
                    fourth,
                    fifth,
                    sixth,
                    iterations
                );
            long extraBytesPerCall = (sixArgumentAllocated - fiveArgumentAllocated) / iterations;

            await Assert
                .That(extraBytesPerCall)
                .IsLessThan(16)
                .Because(
                    $"Five-user-argument chained calls allocated {fiveArgumentAllocated} bytes; six-user-argument chained calls allocated {sixArgumentAllocated} bytes."
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
            LuaValue callableValue = LuaValue.NewTable(callable);
            LuaValue[] args =
            {
                LuaValue.NewNumber(1),
                LuaValue.NewNumber(2),
                LuaValue.NewNumber(3),
                LuaValue.NewNumber(4),
            };
            LuaValue callback = LuaValue.NewCallbackView(
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

                    return LuaValue.Nil;
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
            LuaValue callableValue = LuaValue.NewTable(callable);
            LuaValue inspect = LuaValue.NewCallback((_, args) => SummarizeArguments(args));
            meta.Set("__call", inspect);
            callable.MetaTable = meta;
            LuaValue[] spanArgs =
            {
                LuaValue.Nil,
                LuaValue.NewTuple(LuaValue.NewNumber(2), LuaValue.NewNumber(20)),
                LuaValue.NewNumber(3),
                LuaValue.NewTuple(LuaValue.NewNumber(4), LuaValue.Nil),
            };
            LuaValue[] arrayArgs =
            {
                LuaValue.NewNumber(1),
                LuaValue.Nil,
                LuaValue.NewTuple(LuaValue.NewNumber(2), LuaValue.NewNumber(20)),
                LuaValue.NewNumber(3),
                LuaValue.Void,
            };

            LuaValue spanResult = script.Call(callableValue, spanArgs.AsSpan());
            LuaValue arrayResult = script.Call(callableValue, arrayArgs);

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
            LuaValue callableValue = LuaValue.NewTable(callable);
            LuaValue[] values =
            {
                LuaValue.NewNumber(1),
                LuaValue.Nil,
                LuaValue.NewTuple(LuaValue.NewNumber(2), LuaValue.NewNumber(20)),
                LuaValue.NewNumber(3),
                LuaValue.NewTuple(LuaValue.NewNumber(4), LuaValue.Nil),
            };

            meta.Set(
                "__call",
                LuaValue.NewCallback((_, args) => SummarizeArgumentsSkippingFirst(args))
            );
            callable.MetaTable = meta;
            LuaValue legacyResult = script.CallValues(
                callableValue,
                values[0],
                values[1],
                values[2],
                values[3],
                values[4]
            );

            meta.Set(
                "__call",
                LuaValue.NewCallbackView((_, args) => SummarizeArgumentsSkippingFirst(args))
            );
            LuaValue viewResult = script.CallValues(
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
                LuaValue.NewCallbackView(
                    (_, args) =>
                    {
                        sawSelf = args.Count == 2 && ReferenceEquals(args[0].Table, callable);
                        return LuaValue.NewNumber(args[1].Number + args.Count);
                    }
                )
            );
            callable.MetaTable = meta;

            LuaValue result = script.CallValues(
                LuaValue.NewTable(callable),
                LuaValue.NewNumber(40)
            );

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
                LuaValue.NewCallbackView(
                    (_, args) =>
                    {
                        sawSelf = args.Count == 5 && ReferenceEquals(args[0].Table, callable);
                        for (int i = 1; i < args.Count; i++)
                        {
                            sum += args[i].Number;
                        }

                        return LuaValue.NewNumber(args.Count);
                    }
                )
            );
            callable.MetaTable = meta;

            LuaValue result = script.CallValues(
                LuaValue.NewTable(callable),
                LuaValue.NewNumber(1),
                LuaValue.NewNumber(2),
                LuaValue.NewNumber(3),
                LuaValue.NewNumber(4)
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
            LuaValue function = script.DoString(
                "return function(a, b, c, d, e) return a + b + c + d + e end"
            );
            LuaValue[] args = CreateSequentialArguments(5);

            LuaValue result = script.Call(function, args.AsSpan());

            await Assert.That(result.Number).IsEqualTo(15d).ConfigureAwait(false);
        }

        [global::TUnit.Core.Test]
        [AllLuaVersions]
        public async Task CallWithReadOnlySpanDynValuesPreservesAdjustmentSemantics(
            LuaCompatibilityVersion version
        )
        {
            Script script = new(version, CoreModulePresets.Complete);
            LuaValue inspect = script.DoString(
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
            LuaValue[] args =
            {
                LuaValue.NewNumber(1),
                LuaValue.Nil,
                LuaValue.NewTuple(LuaValue.NewNumber(2), LuaValue.NewNumber(20)),
                LuaValue.NewNumber(3),
                LuaValue.NewTuple(LuaValue.NewNumber(4), LuaValue.Nil),
            };

            LuaValue result = script.Call(inspect, args.AsSpan());

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
            LuaValue callback = LuaValue.NewCallbackView(
                (_, args) =>
                {
                    bool spanAvailable = args.TryGetSpan(out ReadOnlySpan<LuaValue> span);
                    double sum = 0d;
                    for (int i = 0; i < span.Length; i++)
                    {
                        sum += span[i].Number;
                    }

                    return LuaValue.NewTuple(
                        LuaValue.NewBoolean(spanAvailable),
                        LuaValue.NewNumber(span.Length),
                        LuaValue.NewNumber(args.Count),
                        LuaValue.NewNumber(sum)
                    );
                }
            );
            LuaValue[] values = CreateSequentialArguments(arity);

            LuaValue result = script.Call(callback, values.AsSpan());

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
        [global::TUnit.Core.Arguments(0)]
        [global::TUnit.Core.Arguments(1)]
        [global::TUnit.Core.Arguments(2)]
        [global::TUnit.Core.Arguments(3)]
        [global::TUnit.Core.Arguments(4)]
        [global::TUnit.Core.Arguments(5)]
        public async Task CallWithReadOnlySpanDynValuesExposesSpanToNoContextCallbackView(int arity)
        {
            Script script = new(CoreModulePresets.Complete);
            LuaValue callback = LuaValue.NewCallbackView(
                (CallbackArgumentsView args) =>
                {
                    bool spanAvailable = args.TryGetSpan(out ReadOnlySpan<LuaValue> span);
                    double sum = 0d;
                    for (int i = 0; i < span.Length; i++)
                    {
                        sum += span[i].Number;
                    }

                    return LuaValue.NewTuple(
                        LuaValue.NewBoolean(spanAvailable),
                        LuaValue.NewNumber(span.Length),
                        LuaValue.NewNumber(args.Count),
                        LuaValue.NewNumber(sum)
                    );
                }
            );
            LuaValue[] values = CreateSequentialArguments(arity);

            LuaValue result = script.Call(callback, values.AsSpan());

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
            LuaValue callback = LuaValue.NewCallbackView(
                (_, args) =>
                {
                    bool spanAvailable = args.TryGetSpan(out ReadOnlySpan<LuaValue> span);
                    return LuaValue.NewTuple(
                        LuaValue.NewBoolean(spanAvailable),
                        LuaValue.NewNumber(span.Length),
                        LuaValue.NewNumber(args.Count),
                        args[0],
                        args[5]
                    );
                }
            );
            LuaValue[] values =
            {
                LuaValue.Nil,
                LuaValue.NewNumber(2),
                LuaValue.NewTuple(LuaValue.NewNumber(3), LuaValue.NewNumber(30)),
                LuaValue.NewNumber(4),
                LuaValue.NewTuple(LuaValue.NewNumber(5), LuaValue.Nil),
            };

            LuaValue result = script.Call(callback, values.AsSpan());

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
            LuaValue inspect = LuaValue.NewCallback((_, args) => SummarizeArguments(args));
            LuaValue[] values =
            {
                LuaValue.NewNumber(1),
                LuaValue.Nil,
                LuaValue.NewTuple(LuaValue.NewNumber(2), LuaValue.NewNumber(20)),
                LuaValue.NewNumber(3),
                LuaValue.NewTuple(LuaValue.NewNumber(4), LuaValue.Nil),
            };

            LuaValue result = script.Call(inspect, values.AsSpan());

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
            LuaValue[] args = CreateSequentialArguments(5);

            LuaValue result = script.Call(script.Globals.Get("callable"), args.AsSpan());

            await Assert.That(result.Number).IsEqualTo(115d).ConfigureAwait(false);
        }

        [global::TUnit.Core.Test]
        [LuaVersionsUntil(LuaCompatibilityVersion.Lua53)]
        public async Task CallWithReadOnlySpanDynValuesRejectsChainedCallMetamethodsBeforeLua54(
            LuaCompatibilityVersion version
        )
        {
            Script script = new(version, CoreModulePresets.Complete);
            LuaValue target = CreateTableValuedCallChain(
                script,
                (_, _, _) => LuaValue.NewString("unexpected")
            );
            LuaValue[] args = CreateSequentialArguments(5);

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
            LuaValue target = CreateTableValuedCallChain(
                script,
                (targetTable, proxyTable, args) =>
                    SummarizeChainedCallArguments(targetTable, proxyTable, args)
            );
            LuaValue[] args = CreateSequentialArguments(5);

            LuaValue result = script.Call(target, args.AsSpan());

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
            LuaValue notCallable = LuaValue.NewString("nope");

            ArgumentException exception = Assert.Throws<ArgumentException>(() =>
                script.CallValues(notCallable)
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
            LuaValue function = script.Globals.Get("add");

            LuaValue result = script.Call(function, 30, 12);

            await Assert.That(result.Number).IsEqualTo(42d).ConfigureAwait(false);
        }

        [global::TUnit.Core.Test]
        [global::TUnit.Core.Arguments(LuaCompatibilityVersion.Lua51)]
        [global::TUnit.Core.Arguments(LuaCompatibilityVersion.Lua52)]
        [global::TUnit.Core.Arguments(LuaCompatibilityVersion.Lua53)]
        [global::TUnit.Core.Arguments(LuaCompatibilityVersion.Lua54)]
        [global::TUnit.Core.Arguments(LuaCompatibilityVersion.Lua55)]
        [ScriptGlobalOptionsIsolation]
        public async Task FixedObjectCallOverloadsPreserveNilAndArity(
            LuaCompatibilityVersion version
        )
        {
            Script script = new(version, CoreModulePresets.Complete);
            LuaValue capture = script.DoString(
                "return function(...) return select('#', ...), ... end"
            );

            LuaValue oneArgResult = script.Call(capture, (object)null);
            await Assert.That(oneArgResult.Type).IsEqualTo(DataType.Tuple).ConfigureAwait(false);
            await Assert.That(oneArgResult.Tuple.Length).IsEqualTo(2).ConfigureAwait(false);
            await Assert.That(oneArgResult.Tuple[0].Number).IsEqualTo(1d).ConfigureAwait(false);
            await Assert
                .That(oneArgResult.Tuple[1].Type)
                .IsEqualTo(DataType.Nil)
                .ConfigureAwait(false);

            LuaValue threeArgResult = script.Call(capture, (object)null, "value", 42);
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

            LuaValue fourArgResult = script.Call(capture, (object)null, "value", 42, true);
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

            LuaValue sixArgResult = script.Call(capture, (object)null, "value", 42, true, 5d, 6d);
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

            LuaValue sevenArgResult = script.Call(
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

            using ScriptCustomConvertersScope converterScope = ScriptCustomConvertersScope.Clear(
                registry =>
                {
                    registry.SetClrToScriptCustomConversion<int>(
                        (_, value) => LuaValue.NewString("custom-int:" + value)
                    );
                    registry.SetClrToScriptCustomConversion<string>(
                        (_, value) => LuaValue.NewString("custom-string:" + value)
                    );
                }
            );

            LuaValue bareInteger = script.Call(capture, 42);
            LuaValue bareString = script.Call(capture, "value");
            LuaValue exactInteger = script.Call(capture, LuaValue.FromInteger(42));

            await Assert.That(bareInteger.Tuple[0].Number).IsEqualTo(1d).ConfigureAwait(false);
            await Assert
                .That(bareInteger.Tuple[1].String)
                .IsEqualTo("custom-int:42")
                .ConfigureAwait(false);
            await Assert.That(bareString.Tuple[0].Number).IsEqualTo(1d).ConfigureAwait(false);
            await Assert
                .That(bareString.Tuple[1].String)
                .IsEqualTo("custom-string:value")
                .ConfigureAwait(false);
            await Assert.That(exactInteger.Tuple[0].Number).IsEqualTo(1d).ConfigureAwait(false);
            await Assert.That(exactInteger.Tuple[1].Number).IsEqualTo(42d).ConfigureAwait(false);
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
            LuaValue capture = script.DoString(
                "return function(...) return select('#', ...), ... end"
            );
            LuaValue[] args = CreateSequentialArguments(arity);

            LuaValue result = CallFunctionWithFixedArguments(script, capture, args);

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
            LuaValue capture = script.DoString(
                "return function(...) return select('#', ...), ... end"
            );

            LuaValue fixedResult = script.CallValues(
                capture,
                LuaValue.Nil,
                LuaValue.NewString("tail")
            );
            await Assert.That(fixedResult.Type).IsEqualTo(DataType.Tuple).ConfigureAwait(false);
            await Assert.That(fixedResult.Tuple.Length).IsEqualTo(3).ConfigureAwait(false);
            await Assert.That(fixedResult.Tuple[0].Number).IsEqualTo(2d).ConfigureAwait(false);
            await Assert
                .That(fixedResult.Tuple[1].Type)
                .IsEqualTo(DataType.Nil)
                .ConfigureAwait(false);
            await Assert.That(fixedResult.Tuple[2].String).IsEqualTo("tail").ConfigureAwait(false);

            LuaValue arrayResult = script.Call(
                capture,
                new LuaValue[] { LuaValue.Nil, LuaValue.NewString("middle"), LuaValue.Nil }
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

            LuaValue tupleResult = script.CallValues(
                capture,
                LuaValue.NewString("head"),
                LuaValue.NewTuple(LuaValue.Nil, LuaValue.NewString("tail"))
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
            LuaValue capture = script.DoString(
                "return function(...) return select('#', ...), ... end"
            );
            object[] args = new object[] { 1, 2, 3 };

            LuaValue result = script.Call(capture, args);

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
            LuaValue capture = script.DoString(
                "return function(value) return type(value), #value, value[1], value[2] end"
            );
            object[] args = new object[] { 1, 2 };

            LuaValue result = script.Call(capture, (object)args);

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
        public async Task FixedObjectCallOverloadsConvertArgumentsBeforeRejectingNilFunction(
            LuaCompatibilityVersion version
        )
        {
            Script script = new(version, CoreModulePresets.Complete);

            ScriptRuntimeException exception = Assert.Throws<ScriptRuntimeException>(() =>
                script.CallObjectArgumentsCore(LuaValue.Nil, new UnregisteredHostObject())
            );

            await Assert
                .That(exception.Message)
                .Contains("cannot convert clr type")
                .ConfigureAwait(false);
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
            LuaValue capture = script.DoString(
                "return function(...) return select('#', ...), ... end"
            );

            LuaValue oneArgResult = script.CallValues(
                capture,
                LuaValue.NewTuple(LuaValue.NewNumber(1), LuaValue.NewNumber(2))
            );
            await AssertTupleNumbers(oneArgResult, 2d, 1d, 2d).ConfigureAwait(false);

            LuaValue twoArgResult = script.CallValues(
                capture,
                LuaValue.NewTuple(LuaValue.NewNumber(1), LuaValue.NewNumber(2)),
                LuaValue.NewNumber(3)
            );
            await AssertTupleNumbers(twoArgResult, 2d, 1d, 3d).ConfigureAwait(false);

            LuaValue nestedTail = LuaValue.NewTuple(
                LuaValue.NewNumber(3),
                LuaValue.NewTuple(LuaValue.NewNumber(4), LuaValue.NewNumber(5))
            );
            LuaValue threeArgResult = script.CallValues(
                capture,
                LuaValue.NewNumber(1),
                LuaValue.NewNumber(2),
                nestedTail
            );
            await AssertTupleNumbers(threeArgResult, 5d, 1d, 2d, 3d, 4d, 5d).ConfigureAwait(false);

            LuaValue fourArgTail = LuaValue.NewTuple(
                LuaValue.NewNumber(4),
                LuaValue.NewTuple(LuaValue.NewNumber(5), LuaValue.NewNumber(6))
            );
            LuaValue fourArgResult = script.CallValues(
                capture,
                LuaValue.NewNumber(1),
                LuaValue.NewNumber(2),
                LuaValue.NewNumber(3),
                fourArgTail
            );
            await AssertTupleNumbers(fourArgResult, 6d, 1d, 2d, 3d, 4d, 5d, 6d)
                .ConfigureAwait(false);

            LuaValue nonTrailingTuple = LuaValue.NewTuple(
                LuaValue.NewNumber(4),
                LuaValue.NewNumber(5)
            );
            LuaValue trailingTuple = LuaValue.NewTuple(
                LuaValue.NewNumber(6),
                LuaValue.NewTuple(LuaValue.NewNumber(7), LuaValue.NewNumber(8))
            );
            LuaValue mixedTupleResult = script.CallValues(
                capture,
                LuaValue.NewTuple(LuaValue.NewNumber(1), LuaValue.NewNumber(2)),
                LuaValue.NewNumber(3),
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
            LuaValue capture = script.DoString(
                "return function(...) return select('#', ...), ... end"
            );

            LuaValue oneVoid = script.CallValues(capture, LuaValue.Void);
            LuaValue twoVoid = script.CallValues(capture, LuaValue.NewNumber(1), LuaValue.Void);
            LuaValue threeVoid = script.CallValues(
                capture,
                LuaValue.NewNumber(1),
                LuaValue.NewNumber(2),
                LuaValue.Void
            );
            LuaValue fourVoid = script.CallValues(
                capture,
                LuaValue.NewNumber(1),
                LuaValue.NewNumber(2),
                LuaValue.NewNumber(3),
                LuaValue.Void
            );
            LuaValue trailingTupleVoid = script.CallValues(
                capture,
                LuaValue.NewNumber(1),
                LuaValue.NewTuple(LuaValue.NewNumber(2), LuaValue.Void)
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

            LuaValue result = script.Call(closure, 6, 7, -1, 1);

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
            Func<ScriptExecutionContext, CallbackArguments, LuaValue> callback = (ctx, args) =>
                LuaValue.NewNumber(args[0].Number * 2d);

            LuaValue result = script.Call(callback, 21);

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
            LuaValue callback = LuaValue.NewCallback((_, _) => LuaValue.NewString("done"));

            LuaValue coroutine = script.CreateCoroutineValue(callback);
            await Assert.That(coroutine.Type).IsEqualTo(DataType.Thread).ConfigureAwait(false);

            ArgumentException exception = Assert.Throws<ArgumentException>(() =>
                script.CreateCoroutineValue(LuaValue.NewNumber(1))
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

            ArgumentException exception = Assert.Throws<ArgumentException>(() =>
                script.CreateCoroutineValue(LuaValue.Nil)
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
        public async Task RequireModuleWarnsWhenBit32NotSupported(LuaCompatibilityVersion version)
        {
            StubScriptLoader loader = new()
            {
                ModuleSource = "return function() return 'bit32' end",
            };
            List<string> messages = new();
            ScriptOptions options = new() { ScriptLoader = loader, DebugPrint = messages.Add };
            Script script = new(CoreModulePresets.Complete, options);

            LuaValue result = script.RequireModule("bit32");

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

            LuaValue foreignTable = scriptA.DoString("return {}");
            scriptB.DoString("function echo(value) return value end");

            ScriptRuntimeException exception = Assert.Throws<ScriptRuntimeException>(() =>
                scriptB.CallValues(scriptB.Globals.Get("echo"), foreignTable)
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

            LuaValue foreignTable = scriptA.DoString("return {}");
            scriptB.DoString("function echo(a, b, c, d) return d end");

            ScriptRuntimeException exception = Assert.Throws<ScriptRuntimeException>(() =>
                scriptB.CallValues(
                    scriptB.Globals.Get("echo"),
                    LuaValue.Nil,
                    LuaValue.Nil,
                    LuaValue.Nil,
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
            LuaValue foreignFunction = scriptA.DoString("return function() end");

            ScriptRuntimeException exception = Assert.Throws<ScriptRuntimeException>(() =>
                scriptB.CreateCoroutineValue(foreignFunction)
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
            LuaValue coroutine = script.CreateCoroutine(closure);

            LuaValue first = coroutine.Coroutine.Resume();
            LuaValue second = coroutine.Coroutine.Resume();

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
            Func<ScriptExecutionContext, CallbackArguments, LuaValue> callback = (ctx, _) =>
                LuaValue.NewNumber(99d);

            LuaValue coroutineValue = script.CreateCoroutine(callback);
            coroutineValue.Coroutine.OwnerScript = script;
            ScriptExecutionContext context = script.CreateDynamicExecutionContext();

            LuaValue result = coroutineValue.Coroutine.Resume(context);

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

        private static async Task AssertTupleNumbers(LuaValue value, params double[] expected)
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

        private static LuaValue SummarizeArguments(CallbackArguments args)
        {
            double nilCount = 0d;
            double sum = 0d;

            for (int i = 0; i < args.Count; i++)
            {
                LuaValue arg = args[i];
                if (arg.Type == DataType.Nil)
                {
                    nilCount++;
                }
                else
                {
                    sum += arg.Number;
                }
            }

            return LuaValue.NewTuple(
                LuaValue.NewNumber(args.Count),
                LuaValue.NewNumber(nilCount),
                LuaValue.NewNumber(sum)
            );
        }

        private static LuaValue SummarizeArguments(CallbackArgumentsView args)
        {
            double nilCount = 0d;
            double sum = 0d;

            for (int i = 0; i < args.Count; i++)
            {
                LuaValue arg = args[i];
                if (arg.Type == DataType.Nil)
                {
                    nilCount++;
                }
                else
                {
                    sum += arg.Number;
                }
            }

            return LuaValue.NewTuple(
                LuaValue.NewNumber(args.Count),
                LuaValue.NewNumber(nilCount),
                LuaValue.NewNumber(sum)
            );
        }

        private static LuaValue SummarizeArgumentsSkippingFirst(CallbackArguments args)
        {
            double nilCount = 0d;
            double sum = 0d;

            for (int i = 1; i < args.Count; i++)
            {
                LuaValue arg = args[i];
                if (arg.Type == DataType.Nil)
                {
                    nilCount++;
                }
                else
                {
                    sum += arg.Number;
                }
            }

            return LuaValue.NewTuple(
                LuaValue.NewNumber(Math.Max(args.Count - 1, 0)),
                LuaValue.NewNumber(nilCount),
                LuaValue.NewNumber(sum)
            );
        }

        private static LuaValue SummarizeArgumentsSkippingFirst(CallbackArgumentsView args)
        {
            double nilCount = 0d;
            double sum = 0d;

            for (int i = 1; i < args.Count; i++)
            {
                LuaValue arg = args[i];
                if (arg.Type == DataType.Nil)
                {
                    nilCount++;
                }
                else
                {
                    sum += arg.Number;
                }
            }

            return LuaValue.NewTuple(
                LuaValue.NewNumber(Math.Max(args.Count - 1, 0)),
                LuaValue.NewNumber(nilCount),
                LuaValue.NewNumber(sum)
            );
        }

        private static async Task AssertArgumentSummary(
            LuaValue value,
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

        private static LuaValue CreateTableValuedCallChain(
            Script script,
            Func<Table, Table, CallbackArguments, LuaValue> callback
        )
        {
            Table target = new(script);
            Table proxy = new(script);
            Table targetMeta = new(script);
            Table proxyMeta = new(script);

            targetMeta.Set("__call", LuaValue.NewTable(proxy));
            proxyMeta.Set(
                "__call",
                LuaValue.NewCallback((_, args) => callback(target, proxy, args))
            );
            target.MetaTable = targetMeta;
            proxy.MetaTable = proxyMeta;

            return LuaValue.NewTable(target);
        }

        private static LuaValue SummarizeChainedCallArguments(
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

            return LuaValue.NewTuple(
                LuaValue.NewNumber(args.Count),
                LuaValue.NewBoolean(args.Count >= 2 && ReferenceEquals(args[0].Table, proxy)),
                LuaValue.NewBoolean(args.Count >= 2 && ReferenceEquals(args[1].Table, target)),
                LuaValue.NewBoolean(args.IsMethodCall),
                LuaValue.NewNumber(userArgumentSum)
            );
        }

        private static async Task AssertChainedCallSummary(LuaValue value, int userArity)
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

        private static LuaValue CallLegacyCallbackWithSequentialArguments(
            Script script,
            LuaValue callback,
            int arity
        )
        {
            return arity switch
            {
                0 => script.CallValues(callback),
                1 => script.CallValues(callback, LuaValue.NewNumber(1)),
                2 => script.CallValues(callback, LuaValue.NewNumber(1), LuaValue.NewNumber(2)),
                3 => script.CallValues(
                    callback,
                    LuaValue.NewNumber(1),
                    LuaValue.NewNumber(2),
                    LuaValue.NewNumber(3)
                ),
                4 => script.CallValues(
                    callback,
                    LuaValue.NewNumber(1),
                    LuaValue.NewNumber(2),
                    LuaValue.NewNumber(3),
                    LuaValue.NewNumber(4)
                ),
                5 => script.CallValues(
                    callback,
                    LuaValue.NewNumber(1),
                    LuaValue.NewNumber(2),
                    LuaValue.NewNumber(3),
                    LuaValue.NewNumber(4),
                    LuaValue.NewNumber(5)
                ),
                6 => script.CallValues(
                    callback,
                    LuaValue.NewNumber(1),
                    LuaValue.NewNumber(2),
                    LuaValue.NewNumber(3),
                    LuaValue.NewNumber(4),
                    LuaValue.NewNumber(5),
                    LuaValue.NewNumber(6)
                ),
                7 => script.CallValues(
                    callback,
                    LuaValue.NewNumber(1),
                    LuaValue.NewNumber(2),
                    LuaValue.NewNumber(3),
                    LuaValue.NewNumber(4),
                    LuaValue.NewNumber(5),
                    LuaValue.NewNumber(6),
                    LuaValue.NewNumber(7)
                ),
                _ => throw new ArgumentOutOfRangeException(nameof(arity)),
            };
        }

        private static LuaValue[] CreateSequentialArguments(int arity)
        {
            LuaValue[] args = new LuaValue[arity];
            for (int i = 0; i < args.Length; i++)
            {
                args[i] = LuaValue.NewNumber(i + 1d);
            }

            return args;
        }

        private static LuaValue CallFunctionWithFixedArguments(
            Script script,
            LuaValue function,
            LuaValue[] args
        )
        {
            return args.Length switch
            {
                6 => script.CallValues(
                    function,
                    args[0],
                    args[1],
                    args[2],
                    args[3],
                    args[4],
                    args[5]
                ),
                7 => script.CallValues(
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
            LuaValue callback,
            int iterations
        )
        {
            long before = GC.GetAllocatedBytesForCurrentThread();
            for (int i = 0; i < iterations; i++)
            {
                script.CallValues(callback);
            }

            return GC.GetAllocatedBytesForCurrentThread() - before;
        }

        private static long MeasureFixedThreeArgumentLegacyCallbackAllocations(
            Script script,
            LuaValue callback,
            LuaValue first,
            LuaValue second,
            LuaValue third,
            int iterations
        )
        {
            long before = GC.GetAllocatedBytesForCurrentThread();
            for (int i = 0; i < iterations; i++)
            {
                script.CallValues(callback, first, second, third);
            }

            return GC.GetAllocatedBytesForCurrentThread() - before;
        }

        private static long MeasureFixedThreeArgumentCallbackViewAllocations(
            Script script,
            LuaValue callback,
            LuaValue first,
            LuaValue second,
            LuaValue third,
            int iterations
        )
        {
            long before = GC.GetAllocatedBytesForCurrentThread();
            for (int i = 0; i < iterations; i++)
            {
                LuaValue result = script.CallValues(callback, first, second, third);
                if (result != third)
                {
                    throw new InvalidOperationException(
                        "Callback-view context allocation probe returned an unexpected value."
                    );
                }
            }

            return GC.GetAllocatedBytesForCurrentThread() - before;
        }

        private static long MeasureDirectFiveArgumentCallbackViewAllocations(
            Script script,
            LuaValue callback,
            LuaValue self,
            LuaValue first,
            LuaValue second,
            LuaValue third,
            LuaValue fourth,
            int iterations
        )
        {
            long before = GC.GetAllocatedBytesForCurrentThread();
            for (int i = 0; i < iterations; i++)
            {
                LuaValue result = script.CallValues(callback, self, first, second, third, fourth);
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
            LuaValue callable,
            LuaValue first,
            LuaValue second,
            LuaValue third,
            LuaValue fourth,
            int iterations
        )
        {
            long before = GC.GetAllocatedBytesForCurrentThread();
            for (int i = 0; i < iterations; i++)
            {
                LuaValue result = script.CallValues(callable, first, second, third, fourth);
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
            LuaValue callable,
            LuaValue first,
            LuaValue second,
            LuaValue third,
            LuaValue fourth,
            LuaValue fifth,
            int iterations
        )
        {
            long before = GC.GetAllocatedBytesForCurrentThread();
            for (int i = 0; i < iterations; i++)
            {
                LuaValue result = script.CallValues(callable, first, second, third, fourth, fifth);
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
            LuaValue callable,
            LuaValue first,
            LuaValue second,
            LuaValue third,
            int iterations
        )
        {
            long before = GC.GetAllocatedBytesForCurrentThread();
            for (int i = 0; i < iterations; i++)
            {
                LuaValue result = script.CallValues(callable, first, second, third);
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
            LuaValue callable,
            LuaValue first,
            LuaValue second,
            LuaValue third,
            LuaValue fourth,
            int iterations
        )
        {
            long before = GC.GetAllocatedBytesForCurrentThread();
            for (int i = 0; i < iterations; i++)
            {
                LuaValue result = script.CallValues(callable, first, second, third, fourth);
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
            LuaValue callable,
            LuaValue first,
            LuaValue second,
            LuaValue third,
            LuaValue fourth,
            LuaValue fifth,
            int iterations
        )
        {
            long before = GC.GetAllocatedBytesForCurrentThread();
            for (int i = 0; i < iterations; i++)
            {
                LuaValue result = script.CallValues(callable, first, second, third, fourth, fifth);
                if (result.Type != DataType.Nil)
                {
                    throw new InvalidOperationException(
                        "Five-argument chained metamethod allocation probe returned an unexpected value."
                    );
                }
            }

            return GC.GetAllocatedBytesForCurrentThread() - before;
        }

        private static long MeasureFixedSixArgumentCallbackViewChainedMetamethodAllocations(
            Script script,
            LuaValue callable,
            LuaValue first,
            LuaValue second,
            LuaValue third,
            LuaValue fourth,
            LuaValue fifth,
            LuaValue sixth,
            int iterations
        )
        {
            long before = GC.GetAllocatedBytesForCurrentThread();
            for (int i = 0; i < iterations; i++)
            {
                LuaValue result = script.CallValues(
                    callable,
                    first,
                    second,
                    third,
                    fourth,
                    fifth,
                    sixth
                );
                if (result.Type != DataType.Nil)
                {
                    throw new InvalidOperationException(
                        "Six-argument chained metamethod allocation probe returned an unexpected value."
                    );
                }
            }

            return GC.GetAllocatedBytesForCurrentThread() - before;
        }

        private static long MeasureSpanCallbackViewMetamethodAllocations(
            Script script,
            LuaValue callable,
            LuaValue[] args,
            int iterations
        )
        {
            long before = GC.GetAllocatedBytesForCurrentThread();
            for (int i = 0; i < iterations; i++)
            {
                LuaValue result = script.CallValues(callable, args.AsSpan());
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
            LuaValue callable,
            LuaValue[] args,
            int iterations
        )
        {
            long before = GC.GetAllocatedBytesForCurrentThread();
            for (int i = 0; i < iterations; i++)
            {
                LuaValue result = script.CallValues(callable, args);
                if (result.Type != DataType.Nil)
                {
                    throw new InvalidOperationException(
                        "Array metamethod allocation probe returned an unexpected value."
                    );
                }
            }

            return GC.GetAllocatedBytesForCurrentThread() - before;
        }

        private static LuaValue CallWithSpan(Script script, LuaValue function, LuaValue[] args)
        {
            return script.CallValues(function, args.AsSpan());
        }

        private static LuaValue CallWithParamsArray(
            Script script,
            LuaValue function,
            LuaValue[] args
        )
        {
            return script.CallValues(function, args);
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
