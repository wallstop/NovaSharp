namespace NovaSharp.Interpreter.Tests.TUnit.VM
{
    using System;
    using System.Threading.Tasks;
    using global::TUnit.Assertions;
    using NovaSharp.Interpreter;
    using NovaSharp.Interpreter.DataTypes;
    using NovaSharp.Interpreter.Errors;
    using NovaSharp.Interpreter.Execution;
    using NovaSharp.Interpreter.Interop;
    using NovaSharp.Interpreter.Interop.BasicDescriptors;
    using NovaSharp.Interpreter.Tests;

    [ScriptGlobalOptionsIsolation]
    public sealed class ScriptRuntimeExceptionTUnitTests
    {
        [global::TUnit.Core.Test]
        public async Task TableIndexFactoriesReturnStockMessages()
        {
            await Assert
                .That(ScriptRuntimeException.TableIndexIsNil().Message)
                .IsEqualTo("table index is nil");
            await Assert
                .That(ScriptRuntimeException.TableIndexIsNaN().Message)
                .IsEqualTo("table index is NaN");
        }

        [global::TUnit.Core.Test]
        public async Task ConvertToNumberFailedReturnsStageSpecificMessage()
        {
            (int stage, string expected)[] cases =
            {
                (0, "value must be a number"),
                (1, "'for' initial value must be a number"),
                (2, "'for' step must be a number"),
                (3, "'for' limit must be a number"),
                (42, "value must be a number"),
            };

            foreach ((int stage, string expected) in cases)
            {
                ScriptRuntimeException exception = ScriptRuntimeException.ConvertToNumberFailed(
                    stage
                );
                await Assert.That(exception.Message).IsEqualTo(expected);
            }
        }

        [global::TUnit.Core.Test]
        public async Task ConvertObjectFailedIncludesClrTypeName()
        {
            ScriptRuntimeException exception = ScriptRuntimeException.ConvertObjectFailed(
                new SampleClrType()
            );

            await Assert
                .That(exception.Message)
                .IsEqualTo($"cannot convert clr type {typeof(SampleClrType)}");
        }

        [global::TUnit.Core.Test]
        public async Task ConvertObjectFailedIncludesLuaTypeName()
        {
            ScriptRuntimeException exception = ScriptRuntimeException.ConvertObjectFailed(
                DataType.String
            );

            await Assert.That(exception.Message).IsEqualTo("cannot convert a string to a clr type");
        }

        [global::TUnit.Core.Test]
        public async Task ConvertObjectFailedIncludesExpectedClrType()
        {
            ScriptRuntimeException exception = ScriptRuntimeException.ConvertObjectFailed(
                DataType.Boolean,
                typeof(Guid)
            );

            await Assert
                .That(exception.Message)
                .IsEqualTo("cannot convert a boolean to a clr type System.Guid");
        }

        [global::TUnit.Core.Test]
        public async Task UserDataArgumentTypeMismatchHighlightsLuaAndClrTypes()
        {
            ScriptRuntimeException exception = ScriptRuntimeException.UserDataArgumentTypeMismatch(
                DataType.Table,
                typeof(string)
            );

            await Assert
                .That(exception.Message)
                .IsEqualTo(
                    "cannot find a conversion from a NovaSharp table to a clr System.String"
                );
        }

        [global::TUnit.Core.Test]
        public async Task UserDataMissingFieldReportsMissingName()
        {
            ScriptRuntimeException exception = ScriptRuntimeException.UserDataMissingField(
                "Widget",
                "length"
            );

            await Assert
                .That(exception.Message)
                .IsEqualTo("cannot access field length of userdata<Widget>");
        }

        [global::TUnit.Core.Test]
        public async Task CannotResumeNotSuspendedDifferentiatesStates()
        {
            (CoroutineState state, string expected)[] cases =
            {
                (CoroutineState.Dead, "cannot resume dead coroutine"),
                (CoroutineState.Running, "cannot resume non-suspended coroutine"),
                (CoroutineState.ForceSuspended, "cannot resume non-suspended coroutine"),
            };

            foreach ((CoroutineState state, string expected) in cases)
            {
                ScriptRuntimeException exception = ScriptRuntimeException.CannotResumeNotSuspended(
                    state
                );
                await Assert.That(exception.Message).IsEqualTo(expected);
            }
        }

        [global::TUnit.Core.Test]
        public async Task CannotYieldMessagesMatchLuaText()
        {
            await Assert
                .That(ScriptRuntimeException.CannotYield().Message)
                .IsEqualTo("attempt to yield across a CLR-call boundary");
            await Assert
                .That(ScriptRuntimeException.CannotYieldMain().Message)
                .IsEqualTo("attempt to yield from outside a coroutine");
        }

        [global::TUnit.Core.Test]
        public async Task AttemptToCallNonFuncFormatsMessages()
        {
            await Assert
                .That(ScriptRuntimeException.AttemptToCallNonFunc(DataType.Nil).Message)
                .IsEqualTo("attempt to call a nil value");
            await Assert
                .That(ScriptRuntimeException.AttemptToCallNonFunc(DataType.Table, "foo").Message)
                .IsEqualTo("attempt to call a table value near 'foo'");
        }

        [global::TUnit.Core.Test]
        public async Task CloseMetamethodExpectedFormatsProvidedValue()
        {
            ScriptRuntimeException exception = ScriptRuntimeException.CloseMetamethodExpected(
                DynValue.NewBoolean(true)
            );

            await Assert
                .That(exception.Message)
                .IsEqualTo("__close metamethod expected (got boolean)");
        }

        [global::TUnit.Core.Test]
        public async Task CloseMetamethodExpectedTreatsNullAsNil()
        {
            ScriptRuntimeException exception = ScriptRuntimeException.CloseMetamethodExpected(null);

            await Assert.That(exception.Message).IsEqualTo("__close metamethod expected (got nil)");
        }

        [global::TUnit.Core.Test]
        public async Task BitwiseOnNonIntegerDescribesSourceType()
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

            await Assert
                .That(floatException.Message)
                .IsEqualTo("attempt to perform bitwise operation on a float value");
            await Assert
                .That(stringException.Message)
                .IsEqualTo("attempt to perform bitwise operation on a string value");
            await Assert
                .That(tableException.Message)
                .IsEqualTo("attempt to perform bitwise operation on a table value");
        }

        [global::TUnit.Core.Test]
        public async Task BitwiseOnNonIntegerThrowsWhenValueNull()
        {
            ArgumentNullException exception = ExpectException<ArgumentNullException>(() =>
                ScriptRuntimeException.BitwiseOnNonInteger(null)
            );

            await Assert.That(exception.ParamName).IsEqualTo("value");
        }

        [global::TUnit.Core.Test]
        public async Task CompareInvalidTypeReportsMatchingTypes()
        {
            DynValue left = DynValue.NewString("a");
            DynValue right = DynValue.NewString("b");

            ScriptRuntimeException exception = ScriptRuntimeException.CompareInvalidType(
                left,
                right
            );

            await Assert.That(exception.Message).IsEqualTo("attempt to compare two string values");
        }

        [global::TUnit.Core.Test]
        public async Task CompareInvalidTypeReportsMismatchedTypes()
        {
            DynValue left = DynValue.NewTable(new Table(new Script()));
            DynValue right = DynValue.NewBoolean(true);

            ScriptRuntimeException exception = ScriptRuntimeException.CompareInvalidType(
                left,
                right
            );

            await Assert.That(exception.Message).IsEqualTo("attempt to compare table with boolean");
        }

        [global::TUnit.Core.Test]
        public async Task ArithmeticOnNonNumberPrefersRightOperandWhenInvalid()
        {
            DynValue left = DynValue.NewNumber(5);
            DynValue right = DynValue.NewTable(new Table(new Script()));

            ScriptRuntimeException exception = ScriptRuntimeException.ArithmeticOnNonNumber(
                left,
                right
            );

            await Assert
                .That(exception.Message)
                .IsEqualTo("attempt to perform arithmetic on a table value");
        }

        [global::TUnit.Core.Test]
        public async Task ArithmeticOnNonNumberTreatsStringsAsInvalid()
        {
            DynValue left = DynValue.NewString("abc");

            ScriptRuntimeException exception = ScriptRuntimeException.ArithmeticOnNonNumber(
                left,
                null
            );

            await Assert
                .That(exception.Message)
                .IsEqualTo("attempt to perform arithmetic on a string value");
        }

        [global::TUnit.Core.Test]
        public async Task ArithmeticOnNonNumberThrowsWhenRightOperandIsString()
        {
            DynValue left = DynValue.NewNumber(1);
            DynValue right = DynValue.NewString("text");

            ScriptRuntimeException exception = ScriptRuntimeException.ArithmeticOnNonNumber(
                left,
                right
            );

            await Assert
                .That(exception.Message)
                .IsEqualTo("attempt to perform arithmetic on a string value");
        }

        [global::TUnit.Core.Test]
        public async Task ArithmeticOnNonNumberReportsLeftOperandTypeWhenInvalid()
        {
            DynValue left = DynValue.NewTable(new Table(new Script()));

            ScriptRuntimeException exception = ScriptRuntimeException.ArithmeticOnNonNumber(left);

            await Assert
                .That(exception.Message)
                .IsEqualTo("attempt to perform arithmetic on a table value");
        }

        [global::TUnit.Core.Test]
        public async Task ArithmeticOnNonNumberThrowsWhenLeftOperandNull()
        {
            ArgumentNullException exception = ExpectException<ArgumentNullException>(() =>
                ScriptRuntimeException.ArithmeticOnNonNumber(null)
            );

            await Assert.That(exception.ParamName).IsEqualTo("l");
        }

        [global::TUnit.Core.Test]
        public async Task ArithmeticOnNonNumberThrowsInternalErrorWhenBothOperandsValid()
        {
            DynValue left = DynValue.NewNumber(3);
            DynValue right = DynValue.NewNumber(4);

            InternalErrorException exception = ExpectException<InternalErrorException>(() =>
                ScriptRuntimeException.ArithmeticOnNonNumber(left, right)
            );

            await Assert
                .That(exception.Message)
                .IsEqualTo("ArithmeticOnNonNumber - both are numbers");
        }

        [global::TUnit.Core.Test]
        public async Task ConcatOnNonStringRejectsRightOperand()
        {
            DynValue left = DynValue.NewNumber(1);
            DynValue right = DynValue.NewBoolean(true);

            ScriptRuntimeException exception = ScriptRuntimeException.ConcatOnNonString(
                left,
                right
            );

            await Assert
                .That(exception.Message)
                .IsEqualTo("attempt to concatenate a boolean value");
        }

        [global::TUnit.Core.Test]
        public async Task ConcatOnNonStringRejectsLeftOperand()
        {
            DynValue left = DynValue.NewTable(new Table(new Script()));
            DynValue right = DynValue.NewNumber(1);

            ScriptRuntimeException exception = ScriptRuntimeException.ConcatOnNonString(
                left,
                right
            );

            await Assert.That(exception.Message).IsEqualTo("attempt to concatenate a table value");
        }

        [global::TUnit.Core.Test]
        public async Task ConcatOnNonStringThrowsWhenLeftOperandNull()
        {
            ArgumentNullException exception = ExpectException<ArgumentNullException>(() =>
                ScriptRuntimeException.ConcatOnNonString(null, DynValue.Nil)
            );

            await Assert.That(exception.ParamName).IsEqualTo("l");
        }

        [global::TUnit.Core.Test]
        public async Task ConcatOnNonStringThrowsInternalErrorWhenOperandsValid()
        {
            DynValue left = DynValue.NewString("a");
            DynValue right = DynValue.NewString("b");

            InternalErrorException exception = ExpectException<InternalErrorException>(() =>
                ScriptRuntimeException.ConcatOnNonString(left, right)
            );

            await Assert
                .That(exception.Message)
                .IsEqualTo("ConcatOnNonString - both are numbers/strings");
        }

        [global::TUnit.Core.Test]
        public async Task BadArgumentUserDataIncludesAllowNilHint()
        {
            ScriptRuntimeException exception = ScriptRuntimeException.BadArgumentUserData(
                1,
                "foo",
                typeof(string),
                new object(),
                allowNil: true
            );

            await Assert.That(exception.Message).Contains("userdata<String>nil or ");
        }

        [global::TUnit.Core.Test]
        public async Task BadArgumentUserDataOmitsNilHintWhenDisallowed()
        {
            ScriptRuntimeException exception = ScriptRuntimeException.BadArgumentUserData(
                2,
                "bar",
                typeof(int),
                new object(),
                allowNil: false
            );

            await Assert
                .That(exception.Message.Contains("nil or ", StringComparison.Ordinal))
                .IsFalse();
        }

        [global::TUnit.Core.Test]
        public async Task BadArgumentUserDataFormatsNullGotValue()
        {
            ScriptRuntimeException exception = ScriptRuntimeException.BadArgumentUserData(
                0,
                "baz",
                typeof(int),
                got: null,
                allowNil: false
            );

            await Assert
                .That(exception.Message)
                .IsEqualTo("bad argument #1 to 'baz' (userdata<Int32> expected, got null)");
        }

        [global::TUnit.Core.Test]
        public async Task BadArgumentOverloadRespectsAllowNilPrefix()
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

            await Assert
                .That(withNil.Message)
                .IsEqualTo("bad argument #1 to 'foo' (nil or table expected, got userdata)");
            await Assert
                .That(withoutNil.Message)
                .IsEqualTo("bad argument #1 to 'foo' (table expected, got userdata)");
        }

        [global::TUnit.Core.Test]
        public async Task BadArgumentNoNegativeNumbersFormatsMessage()
        {
            ScriptRuntimeException exception = ScriptRuntimeException.BadArgumentNoNegativeNumbers(
                3,
                "bar"
            );

            await Assert
                .That(exception.Message)
                .IsEqualTo("bad argument #4 to 'bar' (not a non-negative number in proper range)");
        }

        [global::TUnit.Core.Test]
        public async Task LenOnInvalidTypeReportsOperandType()
        {
            DynValue value = DynValue.NewBoolean(true);

            ScriptRuntimeException exception = ScriptRuntimeException.LenOnInvalidType(value);

            await Assert
                .That(exception.Message)
                .IsEqualTo("attempt to get length of a boolean value");
        }

        [global::TUnit.Core.Test]
        public async Task LenOnInvalidTypeThrowsWhenOperandNull()
        {
            ArgumentNullException exception = ExpectException<ArgumentNullException>(() =>
                ScriptRuntimeException.LenOnInvalidType(null)
            );

            await Assert.That(exception.ParamName).IsEqualTo("r");
        }

        [global::TUnit.Core.Test]
        public async Task IndexTypeReportsOperandType()
        {
            DynValue value = DynValue.NewTable(new Table(new Script()));

            ScriptRuntimeException exception = ScriptRuntimeException.IndexType(value);

            await Assert.That(exception.Message).IsEqualTo("attempt to index a table value");
        }

        [global::TUnit.Core.Test]
        public async Task IndexTypeThrowsWhenOperandNull()
        {
            ArgumentNullException exception = ExpectException<ArgumentNullException>(() =>
                ScriptRuntimeException.IndexType(null)
            );

            await Assert.That(exception.ParamName).IsEqualTo("obj");
        }

        [global::TUnit.Core.Test]
        public async Task BadArgumentFactoriesProduceExpectedMessages()
        {
            ScriptRuntimeException basic = ScriptRuntimeException.BadArgument(0, "foo", "message");
            ScriptRuntimeException typed = ScriptRuntimeException.BadArgument(
                1,
                "bar",
                DataType.Number,
                DataType.Boolean,
                allowNil: false
            );

            await Assert.That(basic.Message).IsEqualTo("bad argument #1 to 'foo' (message)");
            await Assert
                .That(typed.Message)
                .IsEqualTo("bad argument #2 to 'bar' (number expected, got boolean)");
        }

        [global::TUnit.Core.Test]
        public async Task BadArgumentNoValueAndValueExpectedUseDistinctMessages()
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

            await Assert
                .That(noValue.Message)
                .IsEqualTo("bad argument #3 to 'baz' (userdata expected, got no value)");
            await Assert
                .That(valueExpected.Message)
                .IsEqualTo("bad argument #5 to 'qux' (value expected)");
        }

        [global::TUnit.Core.Test]
        public async Task BadArgumentIndexOutOfRangeReportsFunctionName()
        {
            ScriptRuntimeException exception = ScriptRuntimeException.BadArgumentIndexOutOfRange(
                "insert",
                1
            );

            await Assert
                .That(exception.Message)
                .IsEqualTo("bad argument #2 to 'insert' (index out of range)");
        }

        [global::TUnit.Core.Test]
        public async Task BadArgumentDataTypeOverloadFormatsMessage()
        {
            ScriptRuntimeException exception = ScriptRuntimeException.BadArgument(
                argNum: 1,
                funcName: "foo",
                expected: DataType.Boolean,
                got: DataType.Table,
                allowNil: false
            );

            await Assert
                .That(exception.Message)
                .IsEqualTo("bad argument #2 to 'foo' (boolean expected, got table)");
        }

        [global::TUnit.Core.Test]
        public async Task LoopInMetamethodHelpersReturnStockMessages()
        {
            await Assert
                .That(ScriptRuntimeException.LoopInIndex().Message)
                .IsEqualTo("loop in gettable");
            await Assert
                .That(ScriptRuntimeException.LoopInNewIndex().Message)
                .IsEqualTo("loop in settable");
            await Assert
                .That(ScriptRuntimeException.LoopInCall().Message)
                .IsEqualTo("loop in call");
        }

        [global::TUnit.Core.Test]
        public async Task CannotCloseCoroutineProvidesStateSpecificMessages()
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

            await Assert.That(main.Message).IsEqualTo("attempt to close the main coroutine");
            await Assert.That(running.Message).IsEqualTo("cannot close a running coroutine");
            await Assert
                .That(suspended.Message)
                .IsEqualTo("cannot close coroutine in state suspended");
        }

        [global::TUnit.Core.Test]
        public async Task CannotCloseCoroutineUsesLowercaseNamesForUnknownStates()
        {
            CoroutineState unknownState = Enum.Parse<CoroutineState>("Unknown");

            ScriptRuntimeException exception = ScriptRuntimeException.CannotCloseCoroutine(
                unknownState
            );

            await Assert
                .That(exception.Message)
                .IsEqualTo("cannot close coroutine in state unknown");
        }

        [global::TUnit.Core.Test]
        public async Task AccessInstanceMemberOnStaticsFormatsMemberOnly()
        {
            IMemberDescriptor descriptor = new StubMemberDescriptor("Length", isStatic: false);

            ScriptRuntimeException exception = ScriptRuntimeException.AccessInstanceMemberOnStatics(
                descriptor
            );

            await Assert
                .That(exception.Message)
                .IsEqualTo("attempt to access instance member Length from a static userdata");
        }

        [global::TUnit.Core.Test]
        public async Task AccessInstanceMemberOnStaticsFormatsDescriptorAndMember()
        {
            IUserDataDescriptor typeDescriptor = new StubUserDataDescriptor("Vector3");
            IMemberDescriptor descriptor = new StubMemberDescriptor("Length", isStatic: false);

            ScriptRuntimeException exception = ScriptRuntimeException.AccessInstanceMemberOnStatics(
                typeDescriptor,
                descriptor
            );

            await Assert
                .That(exception.Message)
                .IsEqualTo(
                    "attempt to access instance member Vector3.Length from a static userdata"
                );
        }

        [global::TUnit.Core.Test]
        public async Task AccessInstanceMemberOnStaticsThrowsWhenDescriptorNull()
        {
            ArgumentNullException exception = ExpectException<ArgumentNullException>(() =>
                ScriptRuntimeException.AccessInstanceMemberOnStatics((IMemberDescriptor)null)
            );

            await Assert.That(exception.ParamName).IsEqualTo("desc");
        }

        [global::TUnit.Core.Test]
        public async Task AccessInstanceMemberOnStaticsThrowsWhenTypeDescriptorNull()
        {
            IMemberDescriptor descriptor = new StubMemberDescriptor("Length", isStatic: false);

            ArgumentNullException exception = ExpectException<ArgumentNullException>(() =>
                ScriptRuntimeException.AccessInstanceMemberOnStatics(null, descriptor)
            );

            await Assert.That(exception.ParamName).IsEqualTo("typeDescr");
        }

        [global::TUnit.Core.Test]
        public async Task AccessInstanceMemberOnStaticsThrowsWhenMemberDescriptorNull()
        {
            IUserDataDescriptor typeDescriptor = new StubUserDataDescriptor("Vector3");

            ArgumentNullException exception = ExpectException<ArgumentNullException>(() =>
                ScriptRuntimeException.AccessInstanceMemberOnStatics(typeDescriptor, null)
            );

            await Assert.That(exception.ParamName).IsEqualTo("desc");
        }

        [global::TUnit.Core.Test]
        public async Task BadArgumentUserDataThrowsWhenExpectedTypeNull()
        {
            ArgumentNullException exception = ExpectException<ArgumentNullException>(() =>
                ScriptRuntimeException.BadArgumentUserData(
                    0,
                    "foo",
                    expected: null,
                    got: new object(),
                    allowNil: false
                )
            );

            await Assert.That(exception.ParamName).IsEqualTo("expected");
        }

        [global::TUnit.Core.Test]
        public async Task ConvertObjectFailedThrowsWhenClrTypeNull()
        {
            ArgumentNullException exception = ExpectException<ArgumentNullException>(() =>
                ScriptRuntimeException.ConvertObjectFailed(DataType.String, (Type)null)
            );

            await Assert.That(exception.ParamName).IsEqualTo("t2");
        }

        [global::TUnit.Core.Test]
        public async Task ScriptRuntimeExceptionCtorThrowsWhenInnerExceptionNull()
        {
            ArgumentNullException exception = ExpectException<ArgumentNullException>(() =>
                CreateFromException(null)
            );

            await Assert.That(exception.ParamName).IsEqualTo("ex");
        }

        [global::TUnit.Core.Test]
        public async Task ScriptRuntimeExceptionCopyCtorThrowsWhenSourceNull()
        {
            ArgumentNullException exception = ExpectException<ArgumentNullException>(() =>
                CreateFromRuntimeException(null)
            );

            await Assert.That(exception.ParamName).IsEqualTo("ex");
        }

        [global::TUnit.Core.Test]
        public async Task ScriptRuntimeExceptionCopyCtorPreservesDecoratedMessageAndInnerException()
        {
            ScriptRuntimeException original = new("boom");
            original.DecoratedMessage = "chunk:7: boom";

            ScriptRuntimeException copy = new(original);

            await Assert.That(copy.Message).IsEqualTo("chunk:7: boom");
            await Assert.That(copy.DecoratedMessage).IsEqualTo("chunk:7: boom");
            await Assert.That(copy.DoNotDecorateMessage).IsTrue();
            await Assert.That(ReferenceEquals(copy.InnerException, original)).IsTrue();
        }

        [global::TUnit.Core.Test]
        public async Task RethrowRespectsGlobalOptionsWhenEnabled()
        {
            bool original = Script.GlobalOptions.RethrowExceptionNested;
            Script.GlobalOptions.RethrowExceptionNested = true;

            try
            {
                ScriptRuntimeException exception = new("boom");

                ScriptRuntimeException rethrown = ExpectException<ScriptRuntimeException>(() =>
                    exception.Rethrow()
                );

                await Assert.That(rethrown == exception).IsFalse();
                await Assert.That(rethrown.InnerException).IsEqualTo(exception);
            }
            finally
            {
                Script.GlobalOptions.RethrowExceptionNested = original;
            }
        }

        [global::TUnit.Core.Test]
        public async Task RethrowDoesNothingWhenGlobalOptionDisabled()
        {
            bool original = Script.GlobalOptions.RethrowExceptionNested;
            Script.GlobalOptions.RethrowExceptionNested = false;

            try
            {
                ScriptRuntimeException exception = new("boom");
                bool threw = false;
                try
                {
                    exception.Rethrow();
                }
                catch (ScriptRuntimeException)
                {
                    threw = true;
                }

                await Assert.That(threw).IsFalse();
            }
            finally
            {
                Script.GlobalOptions.RethrowExceptionNested = original;
            }
        }

        [global::TUnit.Core.Test]
        public async Task ConvertObjectFailedThrowsWhenObjectNull()
        {
            ArgumentNullException exception = ExpectException<ArgumentNullException>(() =>
                ScriptRuntimeException.ConvertObjectFailed((object)null)
            );

            await Assert.That(exception.ParamName).IsEqualTo("obj");
        }

        [global::TUnit.Core.Test]
        public async Task UserDataArgumentTypeMismatchThrowsWhenClrTypeNull()
        {
            ArgumentNullException exception = ExpectException<ArgumentNullException>(() =>
                ScriptRuntimeException.UserDataArgumentTypeMismatch(DataType.Number, null)
            );

            await Assert.That(exception.ParamName).IsEqualTo("clrType");
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

        private static TException ExpectException<TException>(Action action)
            where TException : Exception
        {
            try
            {
                action();
            }
            catch (TException ex)
            {
                return ex;
            }

            throw new InvalidOperationException(
                $"Expected exception of type {typeof(TException).Name}."
            );
        }

        private static ScriptRuntimeException CreateFromException(Exception exception) =>
            new(exception);

        private static ScriptRuntimeException CreateFromRuntimeException(
            ScriptRuntimeException exception
        ) => new(exception);
    }
}
