namespace NovaSharp.Interpreter.Tests.Units
{
    using System;
    using NovaSharp.Interpreter;
    using NovaSharp.Interpreter.DataTypes;
    using NovaSharp.Interpreter.Errors;
    using NovaSharp.Interpreter.Execution;
    using NovaSharp.Interpreter.Interop.LuaStateInterop;
    using NUnit.Framework;

    [TestFixture]
    public sealed class LuaBaseTests
    {
        [Test]
        public void LuaTypeMapsNovaSharpValuesAndThrowsOnUnsupported()
        {
            Script script = new Script();
            UserData.RegisterType<SampleUserData>();
            DynValue coroutine = script.CreateCoroutine(
                script.LoadString("return function(...) return ... end").Function
            );

            (DynValue Value, int Expected)[] cases =
            {
                (DynValue.Void, LuaBaseProxy.TNone),
                (DynValue.Nil, LuaBaseProxy.TNil),
                (DynValue.NewNumber(42), LuaBaseProxy.TNumber),
                (UserData.CreateStatic(typeof(SampleUserData)), LuaBaseProxy.TUserData),
                (coroutine, LuaBaseProxy.TThread),
            };

            foreach ((DynValue value, int expected) in cases)
            {
                LuaState state = CreateLuaState(script, value);
                Assert.That(LuaBaseProxy.GetLuaType(state, 1), Is.EqualTo(expected));
            }

            LuaState invalidState = CreateLuaState(script);
            invalidState.Push(DynValue.NewTuple(DynValue.NewNumber(1), DynValue.NewNumber(2)));

            Assert.That(
                () => LuaBaseProxy.GetLuaType(invalidState, 1),
                Throws.TypeOf<ScriptRuntimeException>().With.Message.Contains("Can't call LuaType")
            );
        }

        [Test]
        public void LuaStringHelpersExposeRawStringValues()
        {
            Script script = new Script();
            LuaState state = CreateLuaState(script, DynValue.NewString("abc"));

            uint rawLength;
            string fromCheck = LuaBaseProxy.CheckString(state, 1, out rawLength);
            string fromToString = LuaBaseProxy.ReadString(state, 1);

            Assert.Multiple(() =>
            {
                Assert.That(fromCheck, Is.EqualTo("abc"));
                Assert.That(fromToString, Is.EqualTo("abc"));
                Assert.That(rawLength, Is.EqualTo(3));
            });
        }

        [Test]
        public void LuaIntegerChecksValidateNumbers()
        {
            Script script = new Script();
            LuaState state = CreateLuaState(script, DynValue.NewNumber(1337));

            int strict = LuaBaseProxy.CheckInteger(state, 1);
            int relaxed = LuaBaseProxy.CheckInt(state, 1);

            Assert.Multiple(() =>
            {
                Assert.That(strict, Is.EqualTo(1337));
                Assert.That(relaxed, Is.EqualTo(1337));
            });
        }

        [Test]
        public void LuaArgCheckThrowsWhenConditionFails()
        {
            Script script = new Script();
            LuaState state = CreateLuaState(script, DynValue.NewString("input"));

            Assert.That(
                () => LuaBaseProxy.ArgCheck(state, false, 1, "expected number"),
                Throws.TypeOf<ScriptRuntimeException>().With.Message.Contains("expected number")
            );
        }

        [Test]
        public void LuaGetTableReturnsRawValuesAndThrowsOnNonTables()
        {
            Script script = new Script();
            Table table = new Table(script);
            table.Set("answer", DynValue.NewNumber(42));

            LuaState successState = CreateLuaState(script);
            successState.Push(DynValue.NewTable(table));
            successState.Push(DynValue.NewString("answer"));

            LuaBaseProxy.GetTable(successState, 1);

            Assert.That(successState.Pop().Number, Is.EqualTo(42));

            LuaState failureState = CreateLuaState(script);
            failureState.Push(DynValue.NewNumber(10));
            failureState.Push(DynValue.NewString("answer"));

            Assert.That(
                () => LuaBaseProxy.GetTable(failureState, 1),
                Throws.TypeOf<NotImplementedException>()
            );
        }

        [Test]
        public void LuaCallSupportsMultiReturnAndNilPadding()
        {
            Script script = new Script();

            // MULTRET branch
            LuaState multiReturnState = CreateLuaState(script);
            DynValue multiFunction = DynValue.NewCallback(
                (ctx, args) => DynValue.NewTuple(DynValue.NewNumber(1), DynValue.NewNumber(2))
            );
            multiReturnState.Push(multiFunction);
            multiReturnState.Push(DynValue.NewNumber(10));

            LuaBaseProxy.Call(multiReturnState, 1);

            Assert.Multiple(() =>
            {
                Assert.That(multiReturnState.Pop().Number, Is.EqualTo(2));
                Assert.That(multiReturnState.Pop().Number, Is.EqualTo(1));
            });

            // Nil padding branch
            LuaState paddedState = CreateLuaState(script);
            DynValue singleFunction = DynValue.NewCallback((ctx, args) => DynValue.NewNumber(7));
            paddedState.Push(singleFunction);
            paddedState.Push(DynValue.NewNumber(5));

            LuaBaseProxy.Call(paddedState, 1, 2);

            Assert.Multiple(() =>
            {
                Assert.That(paddedState.Pop(), Is.EqualTo(DynValue.Nil));
                Assert.That(paddedState.Pop().Number, Is.EqualTo(7));
            });
        }

        private static LuaState CreateLuaState(Script script, params DynValue[] args)
        {
            ScriptExecutionContext context = TestHelpers.CreateExecutionContext(script);
            CallbackArguments callbackArguments = TestHelpers.CreateArguments(args);
            return new LuaState(context, callbackArguments, "LuaBaseTests");
        }

        private sealed class LuaBaseProxy : LuaBase
        {
            public static int TNone => LUA_TNONE;
            public static int TNil => LUA_TNIL;
            public static int TNumber => LUA_TNUMBER;
            public static int TUserData => LUA_TUSERDATA;
            public static int TThread => LUA_TTHREAD;

            public static int GetLuaType(LuaState state, int position)
            {
                return LuaType(state, position);
            }

            public static string ReadString(LuaState state, int position) =>
                LuaToString(state, position);

            public static string CheckString(LuaState state, int position, out uint length) =>
                LuaLCheckLString(state, position, out length);

            public static int CheckInteger(LuaState state, int position)
            {
                return LuaLCheckInteger(state, position);
            }

            public static int CheckInt(LuaState state, int position)
            {
                return LuaLCheckInt(state, position);
            }

            public static void ArgCheck(LuaState state, bool condition, int arg, string message)
            {
                LuaLArgCheck(state, condition, arg, message);
            }

            public static void GetTable(LuaState state, int index)
            {
                LuaGetTable(state, index);
            }

            public static void Call(LuaState state, int argCount, int resultCount = -1)
            {
                LuaCall(state, argCount, resultCount);
            }
        }

        private sealed class SampleUserData { }
    }
}
