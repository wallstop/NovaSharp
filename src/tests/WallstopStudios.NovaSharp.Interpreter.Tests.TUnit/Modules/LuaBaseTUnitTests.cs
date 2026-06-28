namespace WallstopStudios.NovaSharp.Interpreter.Tests.TUnit.Modules
{
    using System;
    using System.Collections.Generic;
    using System.Threading.Tasks;
    using global::TUnit.Assertions;
    using WallstopStudios.NovaSharp.Interpreter;
    using WallstopStudios.NovaSharp.Interpreter.DataTypes;
    using WallstopStudios.NovaSharp.Interpreter.Errors;
    using WallstopStudios.NovaSharp.Interpreter.Execution;
    using WallstopStudios.NovaSharp.Interpreter.LuaPort.LuaStateInterop;
    using WallstopStudios.NovaSharp.Interpreter.Tests;
    using WallstopStudios.NovaSharp.Interpreter.Tests.Units;
    using WallstopStudios.NovaSharp.Tests.TestInfrastructure.Scopes;
    using static NovaSharp.Interpreter.LuaPort.LuaStateInterop.LuaBase;

    public sealed class LuaBaseTUnitTests
    {
        static LuaBaseTUnitTests()
        {
            _ = new SampleUserData();
        }

        [global::TUnit.Core.Test]
        public async Task LuaTypeMapsNovaSharpValuesAndThrowsOnUnsupported()
        {
            Script script = new();
            using UserDataRegistrationScope registrationScope =
                UserDataRegistrationScope.Track<SampleUserData>(ensureUnregistered: true);
            registrationScope.RegisterType<SampleUserData>();
            DynValue coroutineValue = script.CreateCoroutine(
                script.LoadString("return function(...) return ... end").Function
            );

            (DynValue Value, int Expected)[] cases =
            {
                (DynValue.Void, LuaBaseProxy.TNone),
                (DynValue.Nil, LuaBaseProxy.TNil),
                (DynValue.NewNumber(42), LuaBaseProxy.TNumber),
                (UserData.CreateStatic<SampleUserData>(), LuaBaseProxy.TUserData),
                (coroutineValue, LuaBaseProxy.TThread),
            };

            foreach ((DynValue value, int expected) in cases)
            {
                LuaState state = CreateLuaState(script, value);
                await Assert.That(LuaBaseProxy.GetLuaType(state, 1)).IsEqualTo(expected);
            }

            LuaState invalidState = CreateLuaState(script);
            invalidState.Push(DynValue.NewTuple(DynValue.NewNumber(1), DynValue.NewNumber(2)));

            ScriptRuntimeException exception = Assert.Throws<ScriptRuntimeException>(() =>
                LuaBaseProxy.GetLuaType(invalidState, 1)
            );
            await Assert.That(exception.Message).Contains("Can't call LuaType");
        }

        [global::TUnit.Core.Test]
        public async Task LuaTypeHandlesBooleansStringsFunctionsAndTables()
        {
            Script script = new();
            DynValue scriptFunction = script.DoString("return function() end");
            DynValue clrFunction = DynValue.NewCallback((_, _) => DynValue.Nil);
            Table table = new(script);

            (DynValue Value, int Expected)[] cases =
            {
                (DynValue.NewBoolean(true), LuaBaseProxy.TNil),
                (script.DoString("return true"), LuaBaseProxy.TNil),
                (DynValue.NewString("txt"), LuaBaseProxy.TString),
                (script.DoString("return 'txt'"), LuaBaseProxy.TString),
                (DynValue.NewTable(table), LuaBaseProxy.TTable),
                (script.DoString("return { key = 'value' }"), LuaBaseProxy.TTable),
                (scriptFunction, LuaBaseProxy.TFunction),
                (clrFunction, LuaBaseProxy.TFunction),
                (script.DoString("return function() end"), LuaBaseProxy.TFunction),
            };

            foreach ((DynValue value, int expected) in cases)
            {
                LuaState state = CreateLuaState(script, value);
                await Assert.That(LuaBaseProxy.GetLuaType(state, 1)).IsEqualTo(expected);
            }
        }

        [global::TUnit.Core.Test]
        public async Task LuaTypeRejectsSyntheticTailCallYieldAndTupleRequests()
        {
            Script script = new();
            DynValue tailCall = DynValue.NewTailCallReq(
                DynValue.NewCallback((_, _) => DynValue.NewNumber(1)),
                DynValue.NewNumber(5)
            );
            DynValue yieldRequest = DynValue.NewYieldReq(new[] { DynValue.NewNumber(6) });
            DynValue tuple = DynValue.NewTuple(DynValue.NewNumber(1), DynValue.NewString("two"));
            DynValue luaTuple = script.DoString("return (function() return 1, 'two' end)()");

            foreach (DynValue value in new[] { tailCall, yieldRequest, tuple, luaTuple })
            {
                LuaState state = CreateLuaState(script);
                state.Push(value);
                ScriptRuntimeException exception = Assert.Throws<ScriptRuntimeException>(() =>
                    LuaBaseProxy.GetLuaType(state, 1)
                );
                await Assert.That(exception.Message).Contains("Can't call LuaType");
            }
        }

        [global::TUnit.Core.Test]
        public async Task LuaTypeClassifiesValuesProducedByLuaScripts()
        {
            Script script = new();
            DynValue boolValue = script.DoString("return true");
            DynValue stringValue = script.DoString("return 'scripted'");
            DynValue tableValue = script.DoString("return { key = 'value' }");
            DynValue functionValue = script.DoString("return function() return 'noop' end");
            DynValue threadValue = script.DoString(
                "return coroutine.create(function() coroutine.yield('hi') end)"
            );

            LuaState state = CreateLuaState(
                script,
                boolValue,
                stringValue,
                tableValue,
                functionValue,
                threadValue
            );

            await Assert.That(LuaBaseProxy.GetLuaType(state, 1)).IsEqualTo(LuaBaseProxy.TNil);
            await Assert.That(LuaBaseProxy.GetLuaType(state, 2)).IsEqualTo(LuaBaseProxy.TString);
            await Assert.That(LuaBaseProxy.GetLuaType(state, 3)).IsEqualTo(LuaBaseProxy.TTable);
            await Assert.That(LuaBaseProxy.GetLuaType(state, 4)).IsEqualTo(LuaBaseProxy.TFunction);
            await Assert.That(LuaBaseProxy.GetLuaType(state, 5)).IsEqualTo(LuaBaseProxy.TThread);
        }

        [global::TUnit.Core.Test]
        public async Task LuaStringHelpersExposeRawStringValues()
        {
            Script script = new();
            DynValue[] values = { DynValue.NewString("abc"), script.DoString("return 'abc'") };

            foreach (DynValue value in values)
            {
                LuaState state = CreateLuaState(script, value);

                uint rawLength;
                string fromCheck = LuaBaseProxy.CheckString(state, 1, out rawLength);
                string fromToString = LuaBaseProxy.ReadString(state, 1);

                await Assert.That(fromCheck).IsEqualTo("abc");
                await Assert.That(fromToString).IsEqualTo("abc");
                await Assert.That(rawLength).IsEqualTo(3u);
            }
        }

        [global::TUnit.Core.Test]
        public async Task LuaIntegerChecksValidateNumbers()
        {
            Script script = new();
            DynValue[] numericValues = { DynValue.NewNumber(1337), script.DoString("return 1337") };

            foreach (DynValue value in numericValues)
            {
                LuaState state = CreateLuaState(script, value);

                int strict = LuaBaseProxy.CheckInteger(state, 1);
                int relaxed = LuaBaseProxy.CheckInt(state, 1);

                await Assert.That(strict).IsEqualTo(1337);
                await Assert.That(relaxed).IsEqualTo(1337);
            }
        }

        [global::TUnit.Core.Test]
        public async Task LuaArgCheckThrowsWhenConditionFails()
        {
            Script script = new();
            DynValue[] payloads =
            {
                DynValue.NewString("input"),
                script.DoString("return 'input'"),
            };

            foreach (DynValue payload in payloads)
            {
                LuaState state = CreateLuaState(script, payload);

                ScriptRuntimeException exception = Assert.Throws<ScriptRuntimeException>(() =>
                    LuaBaseProxy.ArgCheck(state, false, 1, "expected number")
                );
                await Assert.That(exception.Message).Contains("expected number");
            }
        }

        [global::TUnit.Core.Test]
        public async Task LuaGetTableReturnsRawValuesAndThrowsOnNonTables()
        {
            Script script = new();
            Table table = new(script);
            table.Set("answer", DynValue.NewNumber(42));

            DynValue[] tableVariants =
            {
                DynValue.NewTable(table),
                script.DoString("return { answer = 42 }"),
            };

            foreach (DynValue tableValue in tableVariants)
            {
                LuaState successState = CreateLuaState(script);
                successState.Push(tableValue);
                successState.Push(DynValue.NewString("answer"));

                LuaBaseProxy.GetTable(successState, 1);
                await Assert.That(successState.Pop().Number).IsEqualTo(42d);
            }

            DynValue[] nonTableVariants = { DynValue.NewNumber(10), script.DoString("return 10") };

            foreach (DynValue nonTable in nonTableVariants)
            {
                LuaState failureState = CreateLuaState(script);
                failureState.Push(nonTable);
                failureState.Push(DynValue.NewString("answer"));

                NotImplementedException exception = Assert.Throws<NotImplementedException>(() =>
                    LuaBaseProxy.GetTable(failureState, 1)
                );
                await Assert.That(exception).IsNotNull();
            }
        }

        [global::TUnit.Core.Test]
        public async Task LuaCallSupportsMultiReturnAndNilPadding()
        {
            Script script = new();

            DynValue multiFunction = DynValue.NewCallback(
                (_, _) => DynValue.NewTuple(DynValue.NewNumber(1), DynValue.NewNumber(2))
            );
            DynValue luaMultiFunction = script.DoString("return function(input) return 1, 2 end");

            foreach (DynValue function in new[] { multiFunction, luaMultiFunction })
            {
                LuaState multiReturnState = CreateLuaState(script);
                multiReturnState.Push(function);
                multiReturnState.Push(DynValue.NewNumber(10));

                LuaBaseProxy.Call(multiReturnState, 1);

                await Assert.That(multiReturnState.Pop().Number).IsEqualTo(2d);
                await Assert.That(multiReturnState.Pop().Number).IsEqualTo(1d);
            }

            DynValue singleFunction = DynValue.NewCallback((_, _) => DynValue.NewNumber(7));
            DynValue luaSingleFunction = script.DoString("return function(value) return value end");

            foreach (
                (DynValue Function, double Expected) testCase in new[]
                {
                    (singleFunction, 7d),
                    (luaSingleFunction, 5d),
                }
            )
            {
                LuaState paddedState = CreateLuaState(script);
                paddedState.Push(testCase.Function);
                paddedState.Push(DynValue.NewNumber(5));

                LuaBaseProxy.Call(paddedState, 1, 2);

                await Assert.That(paddedState.Pop()).IsEqualTo(DynValue.Nil);
                await Assert.That(paddedState.Pop().Number).IsEqualTo(testCase.Expected);
            }
        }

        [global::TUnit.Core.Test]
        public async Task LuaBufferAndStackHelpersOperateOnValues()
        {
            Script script = new();
            DynValue[] sourceVariants =
            {
                DynValue.NewString("source"),
                script.DoString("return 'source'"),
            };

            foreach (DynValue source in sourceVariants)
            {
                LuaState state = CreateLuaState(
                    script,
                    DynValue.Nil,
                    DynValue.NewNumber(21),
                    source,
                    DynValue.NewBoolean(true)
                );

                await Assert.That(LuaBaseProxy.OptionalInteger(state, 1, 99)).IsEqualTo(99);
                await Assert.That(LuaBaseProxy.OptionalInteger(state, 2, 99)).IsEqualTo(21);
                await Assert.That(LuaBaseProxy.OptionalIntAlias(state, 2, 77)).IsEqualTo(21);
                await Assert.That(LuaBaseProxy.ToBoolean(state, 4)).IsEqualTo(1);
                await Assert.That(LuaBaseProxy.TypeName(state, 3)).IsEqualTo("string");
                await Assert.That(LuaBaseProxy.IsString(state, 2)).IsEqualTo(1);

                LuaBaseProxy.PushInteger(state, 7);
                LuaBaseProxy.PushNilValue(state);
                LuaBaseProxy.PushLiteral(state, "lit");
                LuaBaseProxy.PushValueCopy(state, 3);
                LuaBaseProxy.Pop(state, 1);

                LuaLBuffer buffer = new(state);
                LuaBaseProxy.InitBuffer(state, buffer);
                LuaBaseProxy.AddChar(buffer, 'A');
                LuaBaseProxy.AddString(buffer, "B");
                LuaBaseProxy.AddLString(buffer, new CharPtr("CDEF"), 2);
                state.Push(DynValue.NewString("tail"));
                LuaBaseProxy.AddValue(buffer);
                LuaBaseProxy.PushResult(buffer);
                await Assert.That(state.Pop().String).IsEqualTo("ABCDtail");

                LuaBaseProxy.PushLString(state, new CharPtr("WXYZ"), 3);
                await Assert.That(state.Pop().String).IsEqualTo("WXY");

                CharPtr pointer = LuaBaseProxy.CheckStringPointer(state, 3);
                await Assert.That(pointer.ToString()).IsEqualTo("source");
                await Assert.That(LuaBaseProxy.CheckStringString(state, 3)).IsEqualTo("source");
                await Assert.That(LuaBaseProxy.CheckNumber(state, 2)).IsEqualTo(21d);

                uint rawLength;
                string raw = LuaBaseProxy.ReadRawString(state, 3, out rawLength);
                await Assert.That(rawLength).IsEqualTo(6u);
                await Assert.That(raw).IsEqualTo("source");

                LuaBaseProxy.CheckStack(state, 2, "ok");
                await Assert.That(LuaBaseProxy.QuoteLiteral("foo")).IsEqualTo("'foo'");
            }

            LuaState finalState = CreateLuaState(
                script,
                DynValue.Nil,
                DynValue.NewNumber(21),
                DynValue.NewString("source"),
                DynValue.NewBoolean(true)
            );

            LuaBaseProxy.AssertCondition(false);

            ScriptRuntimeException error = Assert.Throws<ScriptRuntimeException>(() =>
                LuaBaseProxy.ThrowLuaError(finalState, "boom {0}", 1)
            );
            await Assert.That(error).IsNotNull();
        }

        [global::TUnit.Core.Test]
        public async Task LuaCLibraryPrimitivesMirrorDotNetExpectations()
        {
            CharPtr abc = new("abc");
            CharPtr abd = new("abd");

            await Assert.That(LuaBaseProxy.MemoryCompare(abc, abd, 2)).IsEqualTo(0);
            await Assert.That(LuaBaseProxy.MemoryCompareExact(abc, abd, 3)).IsLessThan(0);
            await Assert.That(LuaBaseProxy.MemoryCompareExact(abd, abc, 3)).IsGreaterThan(0);

            CharPtr memSource = new("hello");
            CharPtr firstL = LuaBaseProxy.MemoryCharacter(memSource, 'l', 5);
            await Assert.That(firstL.ToString()).IsEqualTo("llo");
            await Assert.That(LuaBaseProxy.MemoryCharacter(memSource, 'z', 5).IsNull).IsTrue();

            CharPtr strpbrk = LuaBaseProxy.StringBreak(new CharPtr("abcdef"), new CharPtr("xyde"));
            await Assert.That(strpbrk.ToString()).IsEqualTo("def");

            await Assert.That(LuaBaseProxy.IsAlphaChar('A')).IsTrue();
            await Assert.That(LuaBaseProxy.IsAlphaInt('A')).IsTrue();
            await Assert.That(LuaBaseProxy.IsControlChar('\n')).IsTrue();
            await Assert.That(LuaBaseProxy.IsControlInt('\n')).IsTrue();
            await Assert.That(LuaBaseProxy.IsDigitChar('7')).IsTrue();
            await Assert.That(LuaBaseProxy.IsDigitInt('7')).IsTrue();
            await Assert.That(LuaBaseProxy.IsLowerChar('q')).IsTrue();
            await Assert.That(LuaBaseProxy.IsLowerInt('q')).IsTrue();
            await Assert.That(LuaBaseProxy.IsPunctuationChar('!')).IsTrue();
            await Assert.That(LuaBaseProxy.IsPunctuationInt('!')).IsTrue();
            await Assert.That(LuaBaseProxy.IsSpaceChar(' ')).IsTrue();
            await Assert.That(LuaBaseProxy.IsSpaceInt('\t')).IsTrue();
            await Assert.That(LuaBaseProxy.IsUpperChar('Q')).IsTrue();
            await Assert.That(LuaBaseProxy.IsUpperInt('Q')).IsTrue();
            await Assert.That(LuaBaseProxy.IsAlphaNumericChar('9')).IsTrue();
            await Assert.That(LuaBaseProxy.IsAlphaNumericInt('9')).IsTrue();
            await Assert.That(LuaBaseProxy.IsGraphicChar('A')).IsTrue();
            await Assert.That(LuaBaseProxy.IsGraphicInt('A')).IsTrue();
            await Assert.That(LuaBaseProxy.IsHexDigitChar('F')).IsTrue();
            await Assert.That(LuaBaseProxy.IsHexDigitChar('G')).IsFalse();

            await Assert.That(LuaBaseProxy.ToLowerChar('X')).IsEqualTo('x');
            await Assert.That(LuaBaseProxy.ToLowerInt('Y')).IsEqualTo('y');
            await Assert.That(LuaBaseProxy.ToUpperChar('m')).IsEqualTo('M');
            await Assert.That(LuaBaseProxy.ToUpperInt('n')).IsEqualTo('N');

            CharPtr str = new("foobar");
            await Assert.That(LuaBaseProxy.StringChar(str, 'b').ToString()).IsEqualTo("bar");
            await Assert.That(LuaBaseProxy.StringChar(str, 'z').IsNull).IsTrue();

            char[] copyDestination = new char[10];
            CharPtr destinationPtr = new(copyDestination);
            LuaBaseProxy.StringCopy(destinationPtr, new CharPtr("cat"));
            await Assert.That(destinationPtr.ToString()).IsEqualTo("cat");

            char[] paddedDestination = new char[6];
            CharPtr paddedPtr = new(paddedDestination);
            LuaBaseProxy.StringCopyN(paddedPtr, new CharPtr("dog"), 5);
            await Assert.That(paddedPtr.ToString()).IsEqualTo("dog");
            await Assert.That(paddedDestination[4]).IsEqualTo('\0');

            await Assert.That(LuaBaseProxy.StringLength(new CharPtr("lengthy"))).IsEqualTo(7);

            char[] sprintfBuffer = new char[20];
            LuaBaseProxy.StringPrint(
                new CharPtr(sprintfBuffer),
                new CharPtr("%s-%d-%X"),
                "lua",
                42,
                255
            );
            await Assert.That(new CharPtr(sprintfBuffer).ToString()).IsEqualTo("lua-42-FF");
        }

        [global::TUnit.Core.Test]
        public async Task StringFormatHandlesOctalSpecifier()
        {
            char[] buffer = new char[20];
            LuaBaseProxy.StringPrint(new CharPtr(buffer), new CharPtr("%o"), 8);
            await Assert.That(new CharPtr(buffer).ToString()).IsEqualTo("10");

            char[] buffer2 = new char[20];
            LuaBaseProxy.StringPrint(new CharPtr(buffer2), new CharPtr("%o"), 63);
            await Assert.That(new CharPtr(buffer2).ToString()).IsEqualTo("77");
        }

        [global::TUnit.Core.Test]
        public async Task StringFormatHandlesUnsignedSpecifier()
        {
            char[] buffer = new char[20];
            LuaBaseProxy.StringPrint(new CharPtr(buffer), new CharPtr("%u"), -1);
            // When a negative int is treated as unsigned, it becomes a large positive number
            await Assert.That(new CharPtr(buffer).ToString()).IsEqualTo("4294967295");
        }

        [global::TUnit.Core.Test]
        public async Task StringFormatHandlesLowercaseHexSpecifier()
        {
            char[] buffer = new char[20];
            LuaBaseProxy.StringPrint(new CharPtr(buffer), new CharPtr("%x"), 255);
            await Assert.That(new CharPtr(buffer).ToString()).IsEqualTo("ff");
        }

        [global::TUnit.Core.Test]
        public async Task StringFormatHandlesCharacterSpecifier()
        {
            char[] buffer = new char[20];
            LuaBaseProxy.StringPrint(new CharPtr(buffer), new CharPtr("%c"), 65);
            await Assert.That(new CharPtr(buffer).ToString()).IsEqualTo("A");

            char[] buffer2 = new char[20];
            LuaBaseProxy.StringPrint(new CharPtr(buffer2), new CharPtr("%c"), 'B');
            await Assert.That(new CharPtr(buffer2).ToString()).IsEqualTo("B");

            char[] buffer3 = new char[20];
            LuaBaseProxy.StringPrint(new CharPtr(buffer3), new CharPtr("%c"), "Hello");
            await Assert.That(new CharPtr(buffer3).ToString()).IsEqualTo("H");
        }

        [global::TUnit.Core.Test]
        public async Task StringFormatHandlesExponentSpecifiers()
        {
            char[] buffer = new char[30];
            LuaBaseProxy.StringPrint(new CharPtr(buffer), new CharPtr("%e"), 1234.5);
            await Assert.That(new CharPtr(buffer).ToString()).Contains("e");

            char[] buffer2 = new char[30];
            LuaBaseProxy.StringPrint(new CharPtr(buffer2), new CharPtr("%E"), 1234.5);
            await Assert.That(new CharPtr(buffer2).ToString()).Contains("E");
        }

        [global::TUnit.Core.Test]
        public async Task StringFormatHandlesGeneralSpecifiers()
        {
            char[] buffer = new char[30];
            LuaBaseProxy.StringPrint(new CharPtr(buffer), new CharPtr("%g"), 0.00001234);
            await Assert.That(new CharPtr(buffer).ToString().Length).IsGreaterThan(0);

            char[] buffer2 = new char[30];
            LuaBaseProxy.StringPrint(new CharPtr(buffer2), new CharPtr("%G"), 0.00001234);
            await Assert.That(new CharPtr(buffer2).ToString().Length).IsGreaterThan(0);
        }

        [global::TUnit.Core.Test]
        public async Task StringFormatHandlesLongModifier()
        {
            char[] buffer = new char[20];
            LuaBaseProxy.StringPrint(new CharPtr(buffer), new CharPtr("%ld"), 123456);
            await Assert.That(new CharPtr(buffer).ToString()).IsEqualTo("123456");
        }

        [global::TUnit.Core.Test]
        public async Task StringFormatHandlesShortModifier()
        {
            char[] buffer = new char[20];
            LuaBaseProxy.StringPrint(new CharPtr(buffer), new CharPtr("%hd"), (short)32767);
            await Assert.That(new CharPtr(buffer).ToString()).IsEqualTo("32767");
        }

        [global::TUnit.Core.Test]
        public async Task StringFormatHandlesPercentEscape()
        {
            char[] buffer = new char[20];
            LuaBaseProxy.StringPrint(new CharPtr(buffer), new CharPtr("100%%"));
            await Assert.That(new CharPtr(buffer).ToString()).IsEqualTo("100%");
        }

        private static LuaState CreateLuaState(Script script, params DynValue[] args)
        {
            ScriptExecutionContext context = TestHelpers.CreateExecutionContext(script);
            CallbackArguments callbackArguments = TestHelpers.CreateArguments(args);
            return new LuaState(context, callbackArguments, "LuaBaseTUnitTests");
        }

        private static class LuaBaseProxy
        {
            public static int TNone => LuaTypeNone;
            public static int TNil => LuaTypeNil;
            public static int TNumber => LuaTypeNumber;
            public static int TString => LuaTypeString;
            public static int TFunction => LuaTypeFunction;
            public static int TTable => LuaTypeTable;
            public static int TUserData => LuaTypeUserData;
            public static int TThread => LuaTypeThread;

            public static int GetLuaType(LuaState state, int position)
            {
                return LuaType(state, position);
            }

            public static string ReadString(LuaState state, int position) =>
                LuaToString(state, position);

            public static string CheckString(LuaState state, int position, out uint length) =>
                LuaLCheckLString(state, position, out length);

            public static int CheckInteger(LuaState state, int position) =>
                LuaLCheckInteger(state, position);

            public static int CheckInt(LuaState state, int position) =>
                LuaLCheckInt(state, position);

            public static void ArgCheck(LuaState state, bool condition, int arg, string message) =>
                LuaLArgCheck(state, condition, arg, message);

            public static void GetTable(LuaState state, int index) => LuaGetTable(state, index);

            public static void Call(LuaState state, int argCount, int resultCount = -1) =>
                LuaCall(state, argCount, resultCount);

            public static int OptionalInteger(LuaState state, int position, int defaultValue) =>
                LuaLOptInteger(state, position, defaultValue);

            public static int OptionalIntAlias(LuaState state, int position, int defaultValue) =>
                LuaLOptInt(state, position, defaultValue);

            public static int ToBoolean(LuaState state, int position) =>
                LuaToBoolean(state, position);

            public static void PushInteger(LuaState state, int value) =>
                LuaPushInteger(state, value);

            public static void PushNilValue(LuaState state) => LuaPushNil(state);

            public static void PushLiteral(LuaState state, string literal) =>
                LuaPushLiteral(state, literal);

            public static void PushValueCopy(LuaState state, int position) =>
                LuaPushValue(state, position);

            public static void Pop(LuaState state, int count) => LuaPop(state, count);

            public static string TypeName(LuaState state, int position) =>
                LuaLTypeName(state, position);

            public static int IsString(LuaState state, int position) =>
                LuaIsString(state, position);

            public static void CheckStack(LuaState state, int count, string message) =>
                LuaLCheckStack(state, count, message);

            public static string QuoteLiteral(string value) => LuaQuoteLiteral(value);

            public static void AssertCondition(bool condition) => LuaAssert(condition);

            public static CharPtr CheckStringPointer(LuaState state, int position) =>
                LuaLCheckString(state, position);

            public static string CheckStringString(LuaState state, int position) =>
                LuaLCheckStringStr(state, position);

            public static double CheckNumber(LuaState state, int position) =>
                LuaLCheckNumber(state, position);

            public static string ReadRawString(LuaState state, int position, out uint length) =>
                LuaToLString(state, position, out length);

            public static void InitBuffer(LuaState state, LuaLBuffer buffer) =>
                LuaLBuffInit(state, buffer);

            public static void AddChar(LuaLBuffer buffer, char character) =>
                LuaLAddChar(buffer, character);

            public static void AddString(LuaLBuffer buffer, string value) =>
                LuaLAddString(buffer, value);

            public static void AddLString(LuaLBuffer buffer, CharPtr pointer, uint length) =>
                LuaLAddLString(buffer, pointer, length);

            public static void AddValue(LuaLBuffer buffer) => LuaLAddValue(buffer);

            public static void PushResult(LuaLBuffer buffer) => LuaLPushResult(buffer);

            public static void PushLString(LuaState state, CharPtr pointer, uint length) =>
                LuaPushLString(state, pointer, length);

            public static void ThrowLuaError(
                LuaState state,
                string message,
                params object[] args
            ) => LuaLError(state, message, args);

            public static int MemoryCompare(CharPtr left, CharPtr right, uint size) =>
                LuaBase.MemoryCompare(left, right, size);

            public static int MemoryCompareExact(CharPtr left, CharPtr right, int size) =>
                LuaBase.MemoryCompare(left, right, size);

            public static CharPtr MemoryCharacter(CharPtr source, char value, uint count) =>
                MemoryFindCharacter(source, value, count);

            public static CharPtr StringBreak(CharPtr source, CharPtr charset) =>
                StringFindAny(source, charset);

            public static bool IsAlphaChar(char value) => IsAlpha(value);

            public static bool IsAlphaInt(int value) => IsAlpha(value);

            public static bool IsControlChar(char value) => IsControl(value);

            public static bool IsControlInt(int value) => IsControl(value);

            public static bool IsDigitChar(char value) => IsDigit(value);

            public static bool IsDigitInt(int value) => IsDigit(value);

            public static bool IsLowerChar(char value) => IsLower(value);

            public static bool IsLowerInt(int value) => IsLower(value);

            public static bool IsPunctuationChar(char value) => IsPunctuation(value);

            public static bool IsPunctuationInt(int value) => IsPunctuation(value);

            public static bool IsSpaceChar(char value) => IsSpace(value);

            public static bool IsSpaceInt(int value) => IsSpace(value);

            public static bool IsUpperChar(char value) => IsUpper(value);

            public static bool IsUpperInt(int value) => IsUpper(value);

            public static bool IsAlphaNumericChar(char value) => IsAlphanumeric(value);

            public static bool IsAlphaNumericInt(int value) => IsAlphanumeric(value);

            public static bool IsHexDigitChar(char value) => IsHexDigit(value);

            public static bool IsGraphicChar(char value) => IsGraphical(value);

            public static bool IsGraphicInt(int value) => IsGraphical(value);

            public static char ToLowerChar(char value) => ToLower(value);

            public static char ToLowerInt(int value) => ToLower(value);

            public static char ToUpperChar(char value) => ToUpper(value);

            public static char ToUpperInt(int value) => ToUpper(value);

            public static CharPtr StringChar(CharPtr value, char target) =>
                StringFindCharacter(value, target);

            public static CharPtr StringCopy(CharPtr destination, CharPtr source) =>
                LuaBase.StringCopy(destination, source);

            public static CharPtr StringCopyN(CharPtr destination, CharPtr source, int length) =>
                StringCopyWithLength(destination, source, length);

            public static int StringLength(CharPtr value) => LuaBase.StringLength(value);

            public static void StringPrint(CharPtr buffer, CharPtr format, params object[] args) =>
                StringFormat(buffer, format, args);
        }

        private sealed class SampleUserData { }
    }
}
