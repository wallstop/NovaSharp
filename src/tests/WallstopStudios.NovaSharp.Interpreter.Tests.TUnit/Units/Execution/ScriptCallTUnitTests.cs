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
        public async Task FixedDynValueCallToLegacyClrFunctionPreservesTryGetSpan(
            LuaCompatibilityVersion version
        )
        {
            Script script = new(version, CoreModulePresets.Complete);
            DynValue callback = DynValue.NewCallback(
                (_, args) =>
                {
                    bool success = args.TryGetSpan(out ReadOnlySpan<DynValue> span);
                    return DynValue.NewTuple(
                        DynValue.NewBoolean(success),
                        DynValue.NewNumber(span.Length)
                    );
                }
            );

            DynValue result = script.Call(
                callback,
                DynValue.NewNumber(10),
                DynValue.NewNumber(20),
                DynValue.NewNumber(30)
            );

            await Assert.That(result.Type).IsEqualTo(DataType.Tuple).ConfigureAwait(false);
            await Assert.That(result.Tuple[0].Boolean).IsTrue().ConfigureAwait(false);
            await Assert.That(result.Tuple[1].Number).IsEqualTo(3d).ConfigureAwait(false);
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
