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
        public void LuaTypeHandlesBooleansStringsFunctionsAndTables()
        {
            Script script = new Script();
            DynValue scriptFunction = script.DoString("return function() end");
            DynValue clrFunction = DynValue.NewCallback((ctx, args) => DynValue.Nil);
            Table table = new Table(script);

            (DynValue Value, int Expected)[] cases =
            {
                (DynValue.NewBoolean(true), LuaBaseProxy.TNil),
                (DynValue.NewString("txt"), LuaBaseProxy.TString),
                (DynValue.NewTable(table), LuaBaseProxy.TTable),
                (scriptFunction, LuaBaseProxy.TFunction),
                (clrFunction, LuaBaseProxy.TFunction),
            };

            foreach ((DynValue value, int expected) in cases)
            {
                LuaState state = CreateLuaState(script, value);
                Assert.That(LuaBaseProxy.GetLuaType(state, 1), Is.EqualTo(expected));
            }
        }

        [Test]
        public void LuaTypeRejectsSyntheticTailCallYieldAndTupleRequests()
        {
            Script script = new Script();
            DynValue tailCall = DynValue.NewTailCallReq(
                DynValue.NewCallback((ctx, args) => DynValue.NewNumber(1)),
                DynValue.NewNumber(5)
            );
            DynValue yieldRequest = DynValue.NewYieldReq(new[] { DynValue.NewNumber(6) });
            DynValue tuple = DynValue.NewTuple(DynValue.NewNumber(1), DynValue.NewString("two"));

            DynValue[] values = { tailCall, yieldRequest, tuple };

            foreach (DynValue value in values)
            {
                LuaState state = CreateLuaState(script);
                state.Push(value);
                Assert.That(
                    () => LuaBaseProxy.GetLuaType(state, 1),
                    Throws.TypeOf<ScriptRuntimeException>().With.Message.Contains("Can't call LuaType")
                );
            }
        }

        [Test]
        public void LuaTypeClassifiesValuesProducedByLuaScripts()
        {
            Script script = new Script();
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

            Assert.Multiple(() =>
            {
                Assert.That(LuaBaseProxy.GetLuaType(state, 1), Is.EqualTo(LuaBaseProxy.TNil));
                Assert.That(LuaBaseProxy.GetLuaType(state, 2), Is.EqualTo(LuaBaseProxy.TString));
                Assert.That(LuaBaseProxy.GetLuaType(state, 3), Is.EqualTo(LuaBaseProxy.TTable));
                Assert.That(LuaBaseProxy.GetLuaType(state, 4), Is.EqualTo(LuaBaseProxy.TFunction));
                Assert.That(LuaBaseProxy.GetLuaType(state, 5), Is.EqualTo(LuaBaseProxy.TThread));
            });
        }

        [Test]
        public void LuaStringHelpersExposeRawStringValues()
        {
            Script script = new Script();
            DynValue[] values =
            {
                DynValue.NewString("abc"),
                script.DoString("return 'abc'"),
            };

            foreach (DynValue value in values)
            {
                LuaState state = CreateLuaState(script, value);

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
        }

        [Test]
        public void LuaIntegerChecksValidateNumbers()
        {
            Script script = new Script();
            DynValue[] numericValues =
            {
                DynValue.NewNumber(1337),
                script.DoString("return 1337"),
            };

            foreach (DynValue value in numericValues)
            {
                LuaState state = CreateLuaState(script, value);

                int strict = LuaBaseProxy.CheckInteger(state, 1);
                int relaxed = LuaBaseProxy.CheckInt(state, 1);

                Assert.Multiple(() =>
                {
                    Assert.That(strict, Is.EqualTo(1337));
                    Assert.That(relaxed, Is.EqualTo(1337));
                });
            }
        }

        [Test]
        public void LuaArgCheckThrowsWhenConditionFails()
        {
            Script script = new Script();
            DynValue[] payloads =
            {
                DynValue.NewString("input"),
                script.DoString("return 'input'"),
            };

            foreach (DynValue payload in payloads)
            {
                LuaState state = CreateLuaState(script, payload);

                Assert.That(
                    () => LuaBaseProxy.ArgCheck(state, false, 1, "expected number"),
                    Throws.TypeOf<ScriptRuntimeException>().With.Message.Contains("expected number")
                );
            }
        }

        [Test]
        public void LuaGetTableReturnsRawValuesAndThrowsOnNonTables()
        {
            Script script = new Script();
            Table table = new Table(script);
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

                Assert.That(successState.Pop().Number, Is.EqualTo(42));
            }

            DynValue[] nonTableVariants =
            {
                DynValue.NewNumber(10),
                script.DoString("return 10"),
            };

            foreach (DynValue nonTable in nonTableVariants)
            {
                LuaState failureState = CreateLuaState(script);
                failureState.Push(nonTable);
                failureState.Push(DynValue.NewString("answer"));

                Assert.That(
                    () => LuaBaseProxy.GetTable(failureState, 1),
                    Throws.TypeOf<NotImplementedException>()
                );
            }
        }

        [Test]
        public void LuaCallSupportsMultiReturnAndNilPadding()
        {
            Script script = new Script();

            // MULTRET branch
            DynValue multiFunction = DynValue.NewCallback(
                (ctx, args) => DynValue.NewTuple(DynValue.NewNumber(1), DynValue.NewNumber(2))
            );
            DynValue luaMultiFunction = script.DoString("return function(input) return 1, 2 end");

            foreach (DynValue function in new[] { multiFunction, luaMultiFunction })
            {
                LuaState multiReturnState = CreateLuaState(script);
                multiReturnState.Push(function);
                multiReturnState.Push(DynValue.NewNumber(10));

                LuaBaseProxy.Call(multiReturnState, 1);

                Assert.Multiple(() =>
                {
                    Assert.That(multiReturnState.Pop().Number, Is.EqualTo(2));
                    Assert.That(multiReturnState.Pop().Number, Is.EqualTo(1));
                });
            }

            // Nil padding branch
            DynValue singleFunction = DynValue.NewCallback((ctx, args) => DynValue.NewNumber(7));
            DynValue luaSingleFunction = script.DoString("return function(value) return value end");

            foreach ((DynValue Function, double Expected) testCase in new[]
            {
                (singleFunction, 7d),
                (luaSingleFunction, 5d),
            })
            {
                LuaState paddedState = CreateLuaState(script);
                paddedState.Push(testCase.Function);
                paddedState.Push(DynValue.NewNumber(5));

                LuaBaseProxy.Call(paddedState, 1, 2);

                Assert.Multiple(() =>
                {
                    Assert.That(paddedState.Pop(), Is.EqualTo(DynValue.Nil));
                    Assert.That(paddedState.Pop().Number, Is.EqualTo(testCase.Expected));
                });
            }
        }

        [Test]
        public void LuaBufferAndStackHelpersOperateOnValues()
        {
            Script script = new Script();
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

                Assert.Multiple(() =>
                {
                    Assert.That(LuaBaseProxy.OptionalInteger(state, 1, 99), Is.EqualTo(99));
                    Assert.That(LuaBaseProxy.OptionalInteger(state, 2, 99), Is.EqualTo(21));
                    Assert.That(LuaBaseProxy.OptionalIntAlias(state, 2, 77), Is.EqualTo(21));
                });

                Assert.That(LuaBaseProxy.ToBoolean(state, 4), Is.EqualTo(1));
                Assert.That(LuaBaseProxy.TypeName(state, 3), Is.EqualTo("string"));
                Assert.That(LuaBaseProxy.IsString(state, 2), Is.EqualTo(1));

                LuaBaseProxy.PushInteger(state, 7);
                LuaBaseProxy.PushNilValue(state);
                LuaBaseProxy.PushLiteral(state, "lit");
                LuaBaseProxy.PushValueCopy(state, 3);
                LuaBaseProxy.Pop(state, 1);

                LuaLBuffer buffer = new LuaLBuffer(state);
                LuaBaseProxy.InitBuffer(state, buffer);
                LuaBaseProxy.AddChar(buffer, 'A');
                LuaBaseProxy.AddString(buffer, "B");
                LuaBaseProxy.AddLString(buffer, new CharPtr("CDEF"), 2);
                state.Push(DynValue.NewString("tail"));
                LuaBaseProxy.AddValue(buffer);
                LuaBaseProxy.PushResult(buffer);
                Assert.That(state.Pop().String, Is.EqualTo("ABCDtail"));

                LuaBaseProxy.PushLString(state, new CharPtr("WXYZ"), 3);
                Assert.That(state.Pop().String, Is.EqualTo("WXY"));

                CharPtr ptr = LuaBaseProxy.CheckStringPointer(state, 3);
                Assert.That(ptr.ToString(), Is.EqualTo("source"));
                Assert.That(LuaBaseProxy.CheckStringString(state, 3), Is.EqualTo("source"));
                Assert.That(LuaBaseProxy.CheckNumber(state, 2), Is.EqualTo(21));

                uint rawLength;
                string raw = LuaBaseProxy.ReadRawString(state, 3, out rawLength);
                Assert.That(rawLength, Is.EqualTo(6));
                Assert.That(raw, Is.EqualTo("source"));

                LuaBaseProxy.CheckStack(state, 2, "ok");
                Assert.That(LuaBaseProxy.QuoteLiteral("foo"), Is.EqualTo("'foo'"));
            }

            LuaState finalState = CreateLuaState(
                script,
                DynValue.Nil,
                DynValue.NewNumber(21),
                DynValue.NewString("source"),
                DynValue.NewBoolean(true)
            );

            LuaBaseProxy.AssertCondition(false); // no-op path

            Assert.That(
                () => LuaBaseProxy.RaiseLuaError(finalState, "boom {0}", 1),
                Throws.TypeOf<ScriptRuntimeException>()
            );
        }

        [Test]
        public void LuaCLibraryPrimitivesMirrorDotNetExpectations()
        {
            CharPtr abc = new CharPtr("abc");
            CharPtr abd = new CharPtr("abd");

            Assert.Multiple(() =>
            {
                Assert.That(LuaBaseProxy.MemoryCompare(abc, abd, 2), Is.EqualTo(0));
                Assert.That(LuaBaseProxy.MemoryCompareExact(abc, abd, 3), Is.LessThan(0));
                Assert.That(LuaBaseProxy.MemoryCompareExact(abd, abc, 3), Is.GreaterThan(0));
            });

            CharPtr memSource = new CharPtr("hello");
            CharPtr firstL = LuaBaseProxy.MemoryCharacter(memSource, 'l', 5);
            Assert.That(firstL.ToString(), Is.EqualTo("llo"));
            Assert.That(LuaBaseProxy.MemoryCharacter(memSource, 'z', 5), Is.Null);

            CharPtr strpbrk = LuaBaseProxy.StringBreak(new CharPtr("abcdef"), new CharPtr("xyde"));
            Assert.That(strpbrk.ToString(), Is.EqualTo("def"));

            Assert.Multiple(() =>
            {
                Assert.That(LuaBaseProxy.IsAlphaChar('A'), Is.True);
                Assert.That(LuaBaseProxy.IsAlphaInt('A'), Is.True);
                Assert.That(LuaBaseProxy.IsControlChar('\n'), Is.True);
                Assert.That(LuaBaseProxy.IsControlInt('\n'), Is.True);
                Assert.That(LuaBaseProxy.IsDigitChar('7'), Is.True);
                Assert.That(LuaBaseProxy.IsDigitInt('7'), Is.True);
                Assert.That(LuaBaseProxy.IsLowerChar('q'), Is.True);
                Assert.That(LuaBaseProxy.IsLowerInt('q'), Is.True);
                Assert.That(LuaBaseProxy.IsPunctuationChar('!'), Is.True);
                Assert.That(LuaBaseProxy.IsPunctuationInt('!'), Is.True);
                Assert.That(LuaBaseProxy.IsSpaceChar(' '), Is.True);
                Assert.That(LuaBaseProxy.IsSpaceInt('\t'), Is.True);
                Assert.That(LuaBaseProxy.IsUpperChar('Q'), Is.True);
                Assert.That(LuaBaseProxy.IsUpperInt('Q'), Is.True);
                Assert.That(LuaBaseProxy.IsAlphaNumericChar('9'), Is.True);
                Assert.That(LuaBaseProxy.IsAlphaNumericInt('9'), Is.True);
                Assert.That(LuaBaseProxy.IsGraphicChar('A'), Is.True);
                Assert.That(LuaBaseProxy.IsGraphicInt('A'), Is.True);
                Assert.That(LuaBaseProxy.IsHexDigitChar('F'), Is.True);
                Assert.That(LuaBaseProxy.IsHexDigitChar('G'), Is.False);
            });

            Assert.Multiple(() =>
            {
                Assert.That(LuaBaseProxy.ToLowerChar('X'), Is.EqualTo('x'));
                Assert.That(LuaBaseProxy.ToLowerInt('Y'), Is.EqualTo('y'));
                Assert.That(LuaBaseProxy.ToUpperChar('m'), Is.EqualTo('M'));
                Assert.That(LuaBaseProxy.ToUpperInt('n'), Is.EqualTo('N'));
            });

            CharPtr str = new CharPtr("foobar");
            Assert.That(LuaBaseProxy.StringChar(str, 'b').ToString(), Is.EqualTo("bar"));
            Assert.That(LuaBaseProxy.StringChar(str, 'z'), Is.Null);

            char[] copyDestination = new char[10];
            CharPtr destinationPtr = new CharPtr(copyDestination);
            LuaBaseProxy.StringCopy(destinationPtr, new CharPtr("cat"));
            Assert.That(destinationPtr.ToString(), Is.EqualTo("cat"));

            char[] paddedDestination = new char[6];
            CharPtr paddedPtr = new CharPtr(paddedDestination);
            LuaBaseProxy.StringCopyN(paddedPtr, new CharPtr("dog"), 5);
            Assert.That(paddedPtr.ToString(), Is.EqualTo("dog"));
            Assert.That(paddedDestination[4], Is.EqualTo('\0'));

            Assert.That(LuaBaseProxy.StringLength(new CharPtr("lengthy")), Is.EqualTo(7));

            char[] sprintfBuffer = new char[20];
            LuaBaseProxy.StringPrint(
                new CharPtr(sprintfBuffer),
                new CharPtr("%s-%d-%X"),
                "lua",
                42,
                255
            );
            Assert.That(new CharPtr(sprintfBuffer).ToString(), Is.EqualTo("lua-42-FF"));
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
            public static int TString => LUA_TSTRING;
            public static int TFunction => LUA_TFUNCTION;
            public static int TTable => LUA_TTABLE;
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

            public static string QuoteLiteral(string value) => LUA_QL(value);

            public static void AssertCondition(bool condition) => LuaAssert(condition);

            public static CharPtr CheckStringPointer(LuaState state, int position) =>
                LuaLCheckString(state, position);

            public static string CheckStringString(LuaState state, int position) =>
                LuaLCheckStringStr(state, position);

            public static double CheckNumber(LuaState state, int position) =>
                LuaLCheckNumber(state, position);

            public static string ReadRawString(
                LuaState state,
                int position,
                out uint length
            ) => LuaToLString(state, position, out length);

            public static void InitBuffer(LuaState state, LuaLBuffer buffer) =>
                LuaLBuffInit(state, buffer);

            public static void AddChar(LuaLBuffer buffer, char character) =>
                LuaLAddChar(buffer, character);

            public static void AddString(LuaLBuffer buffer, string value) =>
                LuaLAddString(buffer, value);

            public static void AddLString(LuaLBuffer buffer, CharPtr ptr, uint length) =>
                LuaLAddLString(buffer, ptr, length);

            public static void AddValue(LuaLBuffer buffer) => LuaLAddValue(buffer);

            public static void PushResult(LuaLBuffer buffer) => LuaLPushResult(buffer);

            public static void PushLString(LuaState state, CharPtr ptr, uint length) =>
                LuaPushLString(state, ptr, length);

            public static void RaiseLuaError(
                LuaState state,
                string message,
                params object[] args
            ) => LuaLError(state, message, args);

            public static int MemoryCompare(CharPtr left, CharPtr right, uint size)
            {
                return Memcmp(left, right, size);
            }

            public static int MemoryCompareExact(CharPtr left, CharPtr right, int size)
            {
                return Memcmp(left, right, size);
            }

            public static CharPtr MemoryCharacter(CharPtr source, char value, uint count)
            {
                return Memchr(source, value, count);
            }

            public static CharPtr StringBreak(CharPtr source, CharPtr charset)
            {
                return Strpbrk(source, charset);
            }

            public static bool IsAlphaChar(char value) => Isalpha(value);
            public static bool IsAlphaInt(int value) => Isalpha(value);
            public static bool IsControlChar(char value) => Iscntrl(value);
            public static bool IsControlInt(int value) => Iscntrl(value);
            public static bool IsDigitChar(char value) => Isdigit(value);
            public static bool IsDigitInt(int value) => Isdigit(value);
            public static bool IsLowerChar(char value) => Islower(value);
            public static bool IsLowerInt(int value) => Islower(value);
            public static bool IsPunctuationChar(char value) => Ispunct(value);
            public static bool IsPunctuationInt(int value) => Ispunct(value);
            public static bool IsSpaceChar(char value) => Isspace(value);
            public static bool IsSpaceInt(int value) => Isspace(value);
            public static bool IsUpperChar(char value) => Isupper(value);
            public static bool IsUpperInt(int value) => Isupper(value);
            public static bool IsAlphaNumericChar(char value) => Isalnum(value);
            public static bool IsAlphaNumericInt(int value) => Isalnum(value);
            public static bool IsHexDigitChar(char value) => Isxdigit(value);
            public static bool IsGraphicChar(char value) => Isgraph(value);
            public static bool IsGraphicInt(int value) => Isgraph(value);

            public static char ToLowerChar(char value) => Tolower(value);
            public static char ToLowerInt(int value) => Tolower(value);
            public static char ToUpperChar(char value) => Toupper(value);
            public static char ToUpperInt(int value) => Toupper(value);

            public static CharPtr StringChar(CharPtr value, char target) => Strchr(value, target);

            public static CharPtr StringCopy(CharPtr destination, CharPtr source) =>
                Strcpy(destination, source);

            public static CharPtr StringCopyN(CharPtr destination, CharPtr source, int length) =>
                Strncpy(destination, source, length);

            public static int StringLength(CharPtr value) => Strlen(value);

            public static void StringPrint(CharPtr buffer, CharPtr format, params object[] args)
            {
                Sprintf(buffer, format, args);
            }
        }

        private sealed class SampleUserData { }
    }
}
