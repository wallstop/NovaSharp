namespace NovaSharp.Interpreter.Tests.Units
{
    using System;
    using NovaSharp.Interpreter;
    using NovaSharp.Interpreter.DataTypes;
    using NovaSharp.Interpreter.Errors;
    using NovaSharp.Interpreter.Interop;
    using NovaSharp.Interpreter.Interop.BasicDescriptors;
    using NUnit.Framework;

    [TestFixture]
    public sealed class ScriptRuntimeExceptionTests
    {
        [Test]
        public void TableIndexFactoriesReturnStockMessages()
        {
            Assert.Multiple(() =>
            {
                Assert.That(
                    ScriptRuntimeException.TableIndexIsNil().Message,
                    Is.EqualTo("table index is nil")
                );
                Assert.That(
                    ScriptRuntimeException.TableIndexIsNaN().Message,
                    Is.EqualTo("table index is NaN")
                );
            });
        }

        [TestCase(0, "value must be a number")]
        [TestCase(1, "'for' initial value must be a number")]
        [TestCase(2, "'for' step must be a number")]
        [TestCase(3, "'for' limit must be a number")]
        [TestCase(42, "value must be a number")]
        public void ConvertToNumberFailedReturnsStageSpecificMessage(int stage, string expected)
        {
            ScriptRuntimeException exception = ScriptRuntimeException.ConvertToNumberFailed(stage);

            Assert.That(exception.Message, Is.EqualTo(expected));
        }

        [Test]
        public void ConvertObjectFailedIncludesClrTypeName()
        {
            ScriptRuntimeException exception = ScriptRuntimeException.ConvertObjectFailed(
                new SampleClrType()
            );

            Assert.That(
                exception.Message,
                Is.EqualTo($"cannot convert clr type {typeof(SampleClrType)}")
            );
        }

        [Test]
        public void ConvertObjectFailedIncludesLuaTypeName()
        {
            ScriptRuntimeException exception = ScriptRuntimeException.ConvertObjectFailed(
                DataType.String
            );

            Assert.That(exception.Message, Is.EqualTo("cannot convert a string to a clr type"));
        }

        [Test]
        public void ConvertObjectFailedIncludesExpectedClrType()
        {
            ScriptRuntimeException exception = ScriptRuntimeException.ConvertObjectFailed(
                DataType.Boolean,
                typeof(Guid)
            );

            Assert.That(
                exception.Message,
                Is.EqualTo("cannot convert a boolean to a clr type System.Guid")
            );
        }

        [Test]
        public void UserDataArgumentTypeMismatchHighlightsLuaAndClrTypes()
        {
            ScriptRuntimeException exception = ScriptRuntimeException.UserDataArgumentTypeMismatch(
                DataType.Table,
                typeof(string)
            );

            Assert.That(
                exception.Message,
                Is.EqualTo("cannot find a conversion from a NovaSharp table to a clr System.String")
            );
        }

        [Test]
        public void UserDataMissingFieldReportsMissingName()
        {
            ScriptRuntimeException exception = ScriptRuntimeException.UserDataMissingField(
                "Widget",
                "length"
            );

            Assert.That(
                exception.Message,
                Is.EqualTo("cannot access field length of userdata<Widget>")
            );
        }

        [TestCase(CoroutineState.Dead, "cannot resume dead coroutine")]
        [TestCase(CoroutineState.Running, "cannot resume non-suspended coroutine")]
        [TestCase(CoroutineState.ForceSuspended, "cannot resume non-suspended coroutine")]
        public void CannotResumeNotSuspendedDifferentiatesStates(
            CoroutineState state,
            string expected
        )
        {
            ScriptRuntimeException exception = ScriptRuntimeException.CannotResumeNotSuspended(
                state
            );

            Assert.That(exception.Message, Is.EqualTo(expected));
        }

        [Test]
        public void CannotYieldMessagesMatchLuaText()
        {
            Assert.Multiple(() =>
            {
                Assert.That(
                    ScriptRuntimeException.CannotYield().Message,
                    Is.EqualTo("attempt to yield across a CLR-call boundary")
                );
                Assert.That(
                    ScriptRuntimeException.CannotYieldMain().Message,
                    Is.EqualTo("attempt to yield from outside a coroutine")
                );
            });
        }

        [Test]
        public void AttemptToCallNonFuncFormatsMessages()
        {
            Assert.Multiple(() =>
            {
                Assert.That(
                    ScriptRuntimeException.AttemptToCallNonFunc(DataType.Nil).Message,
                    Is.EqualTo("attempt to call a nil value")
                );
                Assert.That(
                    ScriptRuntimeException.AttemptToCallNonFunc(DataType.Table, "foo").Message,
                    Is.EqualTo("attempt to call a table value near 'foo'")
                );
            });
        }

        [Test]
        public void CloseMetamethodExpectedFormatsProvidedValue()
        {
            ScriptRuntimeException exception = ScriptRuntimeException.CloseMetamethodExpected(
                DynValue.NewBoolean(true)
            );

            Assert.That(exception.Message, Is.EqualTo("__close metamethod expected (got boolean)"));
        }

        [Test]
        public void CloseMetamethodExpectedTreatsNullAsNil()
        {
            ScriptRuntimeException exception = ScriptRuntimeException.CloseMetamethodExpected(null);

            Assert.That(exception.Message, Is.EqualTo("__close metamethod expected (got nil)"));
        }

        [Test]
        public void BitwiseOnNonIntegerDescribesSourceType()
        {
            ScriptRuntimeException floatException = ScriptRuntimeException.BitwiseOnNonInteger(
                DynValue.NewNumber(1.5)
            );
            ScriptRuntimeException stringException = ScriptRuntimeException.BitwiseOnNonInteger(
                DynValue.NewString("bits")
            );
            ScriptRuntimeException tableException = ScriptRuntimeException.BitwiseOnNonInteger(
                DynValue.NewTable(new Table(new Script()))
            );

            Assert.Multiple(() =>
            {
                Assert.That(
                    floatException.Message,
                    Is.EqualTo("attempt to perform bitwise operation on a float value")
                );
                Assert.That(
                    stringException.Message,
                    Is.EqualTo("attempt to perform bitwise operation on a string value")
                );
                Assert.That(
                    tableException.Message,
                    Is.EqualTo("attempt to perform bitwise operation on a table value")
                );
            });
        }

        [Test]
        public void CompareInvalidTypeReportsMatchingTypes()
        {
            DynValue left = DynValue.NewString("a");
            DynValue right = DynValue.NewString("b");

            ScriptRuntimeException exception = ScriptRuntimeException.CompareInvalidType(
                left,
                right
            );

            Assert.That(exception.Message, Is.EqualTo("attempt to compare two string values"));
        }

        [Test]
        public void CompareInvalidTypeReportsMismatchedTypes()
        {
            DynValue left = DynValue.NewTable(new Table(new Script()));
            DynValue right = DynValue.NewBoolean(true);

            ScriptRuntimeException exception = ScriptRuntimeException.CompareInvalidType(
                left,
                right
            );

            Assert.That(exception.Message, Is.EqualTo("attempt to compare table with boolean"));
        }

        [Test]
        public void ArithmeticOnNonNumberPrefersRightOperandWhenInvalid()
        {
            DynValue left = DynValue.NewNumber(5);
            DynValue right = DynValue.NewTable(new Table(new Script()));

            ScriptRuntimeException exception = ScriptRuntimeException.ArithmeticOnNonNumber(
                left,
                right
            );

            Assert.That(
                exception.Message,
                Is.EqualTo("attempt to perform arithmetic on a table value")
            );
        }

        [Test]
        public void ArithmeticOnNonNumberTreatsStringsAsInvalid()
        {
            DynValue left = DynValue.NewString("abc");

            ScriptRuntimeException exception = ScriptRuntimeException.ArithmeticOnNonNumber(
                left,
                null
            );

            Assert.That(
                exception.Message,
                Is.EqualTo("attempt to perform arithmetic on a string value")
            );
        }

        [Test]
        public void ArithmeticOnNonNumberThrowsWhenRightOperandIsString()
        {
            DynValue left = DynValue.NewNumber(1);
            DynValue right = DynValue.NewString("text");

            ScriptRuntimeException exception = ScriptRuntimeException.ArithmeticOnNonNumber(
                left,
                right
            );

            Assert.That(
                exception.Message,
                Is.EqualTo("attempt to perform arithmetic on a string value")
            );
        }

        [Test]
        public void ConcatOnNonStringRejectsRightOperand()
        {
            DynValue left = DynValue.NewNumber(1);
            DynValue right = DynValue.NewBoolean(true);

            ScriptRuntimeException exception = ScriptRuntimeException.ConcatOnNonString(
                left,
                right
            );

            Assert.That(exception.Message, Is.EqualTo("attempt to concatenate a boolean value"));
        }

        [Test]
        public void ConcatOnNonStringRejectsLeftOperand()
        {
            DynValue left = DynValue.NewTable(new Table(new Script()));
            DynValue right = DynValue.NewNumber(1);

            ScriptRuntimeException exception = ScriptRuntimeException.ConcatOnNonString(
                left,
                right
            );

            Assert.That(exception.Message, Is.EqualTo("attempt to concatenate a table value"));
        }

        [Test]
        public void BadArgumentUserDataIncludesAllowNilHint()
        {
            ScriptRuntimeException exception = ScriptRuntimeException.BadArgumentUserData(
                1,
                "foo",
                typeof(string),
                new object(),
                allowNil: true
            );

            Assert.That(exception.Message, Does.Contain("userdata<String>nil or "));
        }

        [Test]
        public void BadArgumentOverloadRespectsAllowNilPrefix()
        {
            ScriptRuntimeException withNil = ScriptRuntimeException.BadArgument(
                argNum: 0,
                funcName: "foo",
                expected: "table",
                got: "userdata",
                allowNil: true
            );

            ScriptRuntimeException withoutNil = ScriptRuntimeException.BadArgument(
                argNum: 0,
                funcName: "foo",
                expected: "table",
                got: "userdata",
                allowNil: false
            );

            Assert.Multiple(() =>
            {
                Assert.That(
                    withNil.Message,
                    Is.EqualTo("bad argument #1 to 'foo' (nil or table expected, got userdata)")
                );
                Assert.That(
                    withoutNil.Message,
                    Is.EqualTo("bad argument #1 to 'foo' (table expected, got userdata)")
                );
            });
        }

        [Test]
        public void BadArgumentNoNegativeNumbersFormatsMessage()
        {
            ScriptRuntimeException exception = ScriptRuntimeException.BadArgumentNoNegativeNumbers(
                3,
                "bar"
            );

            Assert.That(
                exception.Message,
                Is.EqualTo("bad argument #4 to 'bar' (not a non-negative number in proper range)")
            );
        }

        [Test]
        public void LenOnInvalidTypeReportsOperandType()
        {
            DynValue value = DynValue.NewBoolean(true);

            ScriptRuntimeException exception = ScriptRuntimeException.LenOnInvalidType(value);

            Assert.That(exception.Message, Is.EqualTo("attempt to get length of a boolean value"));
        }

        [Test]
        public void IndexTypeReportsOperandType()
        {
            DynValue value = DynValue.NewTable(new Table(new Script()));

            ScriptRuntimeException exception = ScriptRuntimeException.IndexType(value);

            Assert.That(exception.Message, Is.EqualTo("attempt to index a table value"));
        }

        [Test]
        public void BadArgumentFactoriesProduceExpectedMessages()
        {
            ScriptRuntimeException basic = ScriptRuntimeException.BadArgument(0, "foo", "message");
            ScriptRuntimeException typed = ScriptRuntimeException.BadArgument(
                1,
                "bar",
                DataType.Number,
                DataType.Boolean,
                allowNil: false
            );

            Assert.Multiple(() =>
            {
                Assert.That(basic.Message, Is.EqualTo("bad argument #1 to 'foo' (message)"));
                Assert.That(
                    typed.Message,
                    Is.EqualTo("bad argument #2 to 'bar' (number expected, got boolean)")
                );
            });
        }

        [Test]
        public void BadArgumentNoValueAndValueExpectedUseDistinctMessages()
        {
            ScriptRuntimeException noValue = ScriptRuntimeException.BadArgumentNoValue(
                2,
                "baz",
                DataType.UserData
            );
            ScriptRuntimeException valueExpected = ScriptRuntimeException.BadArgumentValueExpected(
                4,
                "qux"
            );

            Assert.Multiple(() =>
            {
                Assert.That(
                    noValue.Message,
                    Is.EqualTo("bad argument #3 to 'baz' (userdata expected, got no value)")
                );
                Assert.That(
                    valueExpected.Message,
                    Is.EqualTo("bad argument #5 to 'qux' (value expected)")
                );
            });
        }

        [Test]
        public void BadArgumentIndexOutOfRangeReportsFunctionName()
        {
            ScriptRuntimeException exception = ScriptRuntimeException.BadArgumentIndexOutOfRange(
                "insert",
                1
            );

            Assert.That(
                exception.Message,
                Is.EqualTo("bad argument #2 to 'insert' (index out of range)")
            );
        }

        [Test]
        public void BadArgumentDataTypeOverloadFormatsMessage()
        {
            ScriptRuntimeException exception = ScriptRuntimeException.BadArgument(
                argNum: 1,
                funcName: "foo",
                expected: DataType.Boolean,
                got: DataType.Table,
                allowNil: false
            );

            Assert.That(
                exception.Message,
                Is.EqualTo("bad argument #2 to 'foo' (boolean expected, got table)")
            );
        }

        [Test]
        public void LoopInMetamethodHelpersReturnStockMessages()
        {
            Assert.Multiple(() =>
            {
                Assert.That(
                    ScriptRuntimeException.LoopInIndex().Message,
                    Is.EqualTo("loop in gettable")
                );
                Assert.That(
                    ScriptRuntimeException.LoopInNewIndex().Message,
                    Is.EqualTo("loop in settable")
                );
                Assert.That(
                    ScriptRuntimeException.LoopInCall().Message,
                    Is.EqualTo("loop in call")
                );
            });
        }

        [Test]
        public void CannotCloseCoroutineProvidesStateSpecificMessages()
        {
            ScriptRuntimeException main = ScriptRuntimeException.CannotCloseCoroutine(
                CoroutineState.Main
            );
            ScriptRuntimeException running = ScriptRuntimeException.CannotCloseCoroutine(
                CoroutineState.Running
            );
            ScriptRuntimeException suspended = ScriptRuntimeException.CannotCloseCoroutine(
                CoroutineState.Suspended
            );

            Assert.Multiple(() =>
            {
                Assert.That(main.Message, Is.EqualTo("attempt to close the main coroutine"));
                Assert.That(running.Message, Is.EqualTo("cannot close a running coroutine"));
                Assert.That(
                    suspended.Message,
                    Is.EqualTo("cannot close coroutine in state suspended")
                );
            });
        }

        [Test]
        public void AccessInstanceMemberOnStaticsFormatsMemberOnly()
        {
            IMemberDescriptor descriptor = new StubMemberDescriptor("Length", isStatic: false);

            ScriptRuntimeException exception = ScriptRuntimeException.AccessInstanceMemberOnStatics(
                descriptor
            );

            Assert.That(
                exception.Message,
                Is.EqualTo("attempt to access instance member Length from a static userdata")
            );
        }

        [Test]
        public void AccessInstanceMemberOnStaticsFormatsDescriptorAndMember()
        {
            IUserDataDescriptor typeDescriptor = new StubUserDataDescriptor("Vector3");
            IMemberDescriptor descriptor = new StubMemberDescriptor("Length", isStatic: false);

            ScriptRuntimeException exception = ScriptRuntimeException.AccessInstanceMemberOnStatics(
                typeDescriptor,
                descriptor
            );

            Assert.That(
                exception.Message,
                Is.EqualTo(
                    "attempt to access instance member Vector3.Length from a static userdata"
                )
            );
        }

        [Test]
        public void BadArgumentUserDataThrowsWhenExpectedTypeNull()
        {
            Assert.That(
                () =>
                    ScriptRuntimeException.BadArgumentUserData(
                        0,
                        "foo",
                        expected: null,
                        got: new object(),
                        allowNil: false
                    ),
                Throws.ArgumentNullException.With.Property("ParamName").EqualTo("expected")
            );
        }

        [Test]
        public void ConvertObjectFailedThrowsWhenClrTypeNull()
        {
            Assert.That(
                () => ScriptRuntimeException.ConvertObjectFailed(DataType.String, (Type)null),
                Throws.ArgumentNullException.With.Property("ParamName").EqualTo("t2")
            );
        }

        [Test]
        public void ScriptRuntimeExceptionCtorThrowsWhenInnerExceptionNull()
        {
            Assert.That(
                () => new ScriptRuntimeException((Exception)null),
                Throws.ArgumentNullException.With.Property("ParamName").EqualTo("ex")
            );
        }

        [Test]
        public void ScriptRuntimeExceptionCopyCtorThrowsWhenSourceNull()
        {
            Assert.That(
                () => new ScriptRuntimeException((ScriptRuntimeException)null),
                Throws.ArgumentNullException.With.Property("ParamName").EqualTo("ex")
            );
        }

        [Test]
        public void RethrowRespectsGlobalOptionsWhenEnabled()
        {
            bool original = Script.GlobalOptions.RethrowExceptionNested;
            Script.GlobalOptions.RethrowExceptionNested = true;

            try
            {
                ScriptRuntimeException exception = new("boom");

                ScriptRuntimeException rethrown = Assert.Throws<ScriptRuntimeException>(() =>
                    exception.Rethrow()
                );

                Assert.Multiple(() =>
                {
                    Assert.That(rethrown, Is.Not.SameAs(exception));
                    Assert.That(rethrown.InnerException, Is.SameAs(exception));
                });
            }
            finally
            {
                Script.GlobalOptions.RethrowExceptionNested = original;
            }
        }

        [Test]
        public void RethrowDoesNothingWhenGlobalOptionDisabled()
        {
            bool original = Script.GlobalOptions.RethrowExceptionNested;
            Script.GlobalOptions.RethrowExceptionNested = false;

            try
            {
                ScriptRuntimeException exception = new("boom");
                Assert.That(() => exception.Rethrow(), Throws.Nothing);
            }
            finally
            {
                Script.GlobalOptions.RethrowExceptionNested = original;
            }
        }

        [Test]
        public void ConvertObjectFailedThrowsWhenObjectNull()
        {
            Assert.That(
                () => ScriptRuntimeException.ConvertObjectFailed((object)null),
                Throws.ArgumentNullException.With.Property("ParamName").EqualTo("obj")
            );
        }

        [Test]
        public void UserDataArgumentTypeMismatchThrowsWhenClrTypeNull()
        {
            Assert.That(
                () => ScriptRuntimeException.UserDataArgumentTypeMismatch(DataType.Number, null),
                Throws.ArgumentNullException.With.Property("ParamName").EqualTo("clrType")
            );
        }

        private sealed class SampleClrType { }

        private sealed class StubMemberDescriptor : IMemberDescriptor
        {
            public StubMemberDescriptor(string name, bool isStatic)
            {
                Name = name;
                IsStatic = isStatic;
            }

            public bool IsStatic { get; }

            public string Name { get; }

            public MemberDescriptorAccess MemberAccess => MemberDescriptorAccess.CanRead;

            public DynValue GetValue(Script script, object obj) =>
                throw new NotSupportedException();

            public void SetValue(Script script, object obj, DynValue value) =>
                throw new NotSupportedException();
        }

        private sealed class StubUserDataDescriptor : IUserDataDescriptor
        {
            public StubUserDataDescriptor(string name)
            {
                Name = name;
            }

            public string Name { get; }

            public Type Type => typeof(object);

            public DynValue Index(
                Script script,
                object obj,
                DynValue index,
                bool isDirectIndexing
            ) => throw new NotSupportedException();

            public bool SetIndex(
                Script script,
                object obj,
                DynValue index,
                DynValue value,
                bool isDirectIndexing
            ) => throw new NotSupportedException();

            public string AsString(object obj) => throw new NotSupportedException();

            public DynValue MetaIndex(Script script, object obj, string metaname) =>
                throw new NotSupportedException();

            public bool IsTypeCompatible(Type type, object obj) =>
                throw new NotSupportedException();
        }
    }
}
