namespace WallstopStudios.NovaSharp.Interpreter.Tests.TUnit.Units.Execution.ScriptExecution
{
    using System;
    using System.Collections.Generic;
    using System.Threading.Tasks;
    using global::TUnit.Assertions;
    using WallstopStudios.NovaSharp.Interpreter;
    using WallstopStudios.NovaSharp.Interpreter.Compatibility;
    using WallstopStudios.NovaSharp.Interpreter.DataTypes;
    using WallstopStudios.NovaSharp.Interpreter.Errors;
    using WallstopStudios.NovaSharp.Interpreter.Interop;
    using WallstopStudios.NovaSharp.Interpreter.Interop.BasicDescriptors;
    using WallstopStudios.NovaSharp.Interpreter.Modules;
    using WallstopStudios.NovaSharp.Interpreter.Tests;
    using WallstopStudios.NovaSharp.Tests.TestInfrastructure.Scopes;
    using WallstopStudios.NovaSharp.Tests.TestInfrastructure.TUnit;

    [ScriptGlobalOptionsIsolation]
    public sealed class ScriptRuntimeExceptionTUnitTests
    {
        // ====== Arithmetic exceptions ======

        [global::TUnit.Core.Test]
        public async Task ArithmeticOnNonNumberThrowsArgumentNullExceptionForNullLeft()
        {
            await Assert
                .That(() => ScriptRuntimeException.ArithmeticOnNonNumber(null))
                .ThrowsExactly<ArgumentNullException>()
                .ConfigureAwait(false);
        }

        [global::TUnit.Core.Test]
        [AllLuaVersions]
        public async Task ArithmeticOnNonNumberReturnsExceptionWhenLeftIsNonNumeric(
            LuaCompatibilityVersion version
        )
        {
            DynValue left = DynValue.NewTable(new Table(new Script(version)));
            ScriptRuntimeException ex = ScriptRuntimeException.ArithmeticOnNonNumber(left);
            await Assert
                .That(ex.Message)
                .IsEqualTo("attempt to perform arithmetic on a table value");
        }

        [global::TUnit.Core.Test]
        public async Task ArithmeticOnNonNumberReturnsExceptionWhenRightIsNonNumeric()
        {
            DynValue left = DynValue.NewNumber(5);
            DynValue right = DynValue.NewBoolean(true);
            ScriptRuntimeException ex = ScriptRuntimeException.ArithmeticOnNonNumber(left, right);
            await Assert
                .That(ex.Message)
                .IsEqualTo("attempt to perform arithmetic on a boolean value");
        }

        [global::TUnit.Core.Test]
        public async Task ArithmeticOnNonNumberReturnsExceptionForStringsAttemptingArithmetic()
        {
            DynValue left = DynValue.NewString("abc");
            ScriptRuntimeException ex = ScriptRuntimeException.ArithmeticOnNonNumber(left);
            await Assert
                .That(ex.Message)
                .IsEqualTo("attempt to perform arithmetic on a string value");
        }

        [global::TUnit.Core.Test]
        public async Task ArithmeticOnNonNumberThrowsInternalErrorWhenBothAreNumbers()
        {
            DynValue left = DynValue.NewNumber(5);
            DynValue right = DynValue.NewNumber(10);
            await Assert
                .That(() => ScriptRuntimeException.ArithmeticOnNonNumber(left, right))
                .ThrowsExactly<InternalErrorException>()
                .ConfigureAwait(false);
        }

        // ====== Bitwise exceptions ======

        [global::TUnit.Core.Test]
        public async Task BitwiseOnNonIntegerThrowsArgumentNullExceptionForNull()
        {
            await Assert
                .That(() => ScriptRuntimeException.BitwiseOnNonInteger(null))
                .ThrowsExactly<ArgumentNullException>()
                .ConfigureAwait(false);
        }

        [global::TUnit.Core.Test]
        public async Task BitwiseOnNonIntegerReturnsDescriptorForString()
        {
            DynValue value = DynValue.NewString("abc");
            ScriptRuntimeException ex = ScriptRuntimeException.BitwiseOnNonInteger(value);
            await Assert
                .That(ex.Message)
                .IsEqualTo("attempt to perform bitwise operation on a string value");
        }

        [global::TUnit.Core.Test]
        public async Task BitwiseOnNonIntegerReturnsDescriptorForBoolean()
        {
            DynValue value = DynValue.NewBoolean(true);
            ScriptRuntimeException ex = ScriptRuntimeException.BitwiseOnNonInteger(value);
            await Assert
                .That(ex.Message)
                .IsEqualTo("attempt to perform bitwise operation on a boolean value");
        }

        // ====== Concat exceptions ======

        [global::TUnit.Core.Test]
        public async Task ConcatOnNonStringThrowsArgumentNullExceptionForNullLeft()
        {
            await Assert
                .That(() => ScriptRuntimeException.ConcatOnNonString(null, DynValue.NewNumber(5)))
                .ThrowsExactly<ArgumentNullException>()
                .ConfigureAwait(false);
        }

        [global::TUnit.Core.Test]
        [AllLuaVersions]
        public async Task ConcatOnNonStringReturnsExceptionWhenLeftIsNonConcatenable(
            LuaCompatibilityVersion version
        )
        {
            DynValue left = DynValue.NewTable(new Table(new Script(version)));
            DynValue right = DynValue.NewString("abc");
            ScriptRuntimeException ex = ScriptRuntimeException.ConcatOnNonString(left, right);
            await Assert.That(ex.Message).IsEqualTo("attempt to concatenate a table value");
        }

        [global::TUnit.Core.Test]
        public async Task ConcatOnNonStringReturnsExceptionWhenRightIsNonConcatenable()
        {
            DynValue left = DynValue.NewString("abc");
            DynValue right = DynValue.NewBoolean(false);
            ScriptRuntimeException ex = ScriptRuntimeException.ConcatOnNonString(left, right);
            await Assert.That(ex.Message).IsEqualTo("attempt to concatenate a boolean value");
        }

        [global::TUnit.Core.Test]
        public async Task ConcatOnNonStringThrowsInternalErrorWhenBothAreConcatenable()
        {
            DynValue left = DynValue.NewString("abc");
            DynValue right = DynValue.NewNumber(123);
            await Assert
                .That(() => ScriptRuntimeException.ConcatOnNonString(left, right))
                .ThrowsExactly<InternalErrorException>()
                .ConfigureAwait(false);
        }

        // ====== Len exceptions ======

        [global::TUnit.Core.Test]
        public async Task LenOnInvalidTypeThrowsArgumentNullExceptionForNull()
        {
            await Assert
                .That(() => ScriptRuntimeException.LenOnInvalidType(null))
                .ThrowsExactly<ArgumentNullException>()
                .ConfigureAwait(false);
        }

        // ====== Compare exceptions ======

        [global::TUnit.Core.Test]
        public async Task CompareInvalidTypeThrowsArgumentNullExceptionForNullLeft()
        {
            await Assert
                .That(() => ScriptRuntimeException.CompareInvalidType(null, DynValue.NewNumber(5)))
                .ThrowsExactly<ArgumentNullException>()
                .ConfigureAwait(false);
        }

        [global::TUnit.Core.Test]
        public async Task CompareInvalidTypeThrowsArgumentNullExceptionForNullRight()
        {
            await Assert
                .That(() => ScriptRuntimeException.CompareInvalidType(DynValue.NewNumber(5), null))
                .ThrowsExactly<ArgumentNullException>()
                .ConfigureAwait(false);
        }

        // ====== Index and Loop exceptions ======

        [global::TUnit.Core.Test]
        public async Task IndexTypeThrowsArgumentNullExceptionForNull()
        {
            await Assert
                .That(() => ScriptRuntimeException.IndexType(null))
                .ThrowsExactly<ArgumentNullException>()
                .ConfigureAwait(false);
        }

        [global::TUnit.Core.Test]
        public async Task IndexTypeReturnsMessageWithoutVariableDescription()
        {
            ScriptRuntimeException ex = ScriptRuntimeException.IndexType(DynValue.Nil);
            await Assert
                .That(ex.Message)
                .IsEqualTo("attempt to index a nil value")
                .ConfigureAwait(false);
        }

        [global::TUnit.Core.Test]
        public async Task IndexTypeReturnsMessageWithVariableDescription()
        {
            ScriptRuntimeException ex = ScriptRuntimeException.IndexType(
                DynValue.Nil,
                "global 'foo'"
            );
            await Assert
                .That(ex.Message)
                .IsEqualTo("attempt to index a nil value (global 'foo')")
                .ConfigureAwait(false);
        }

        [global::TUnit.Core.Test]
        public async Task IndexTypeIncludesLocalVariableDescription()
        {
            ScriptRuntimeException ex = ScriptRuntimeException.IndexType(
                DynValue.Nil,
                "local 'bar'"
            );
            await Assert
                .That(ex.Message)
                .IsEqualTo("attempt to index a nil value (local 'bar')")
                .ConfigureAwait(false);
        }

        [global::TUnit.Core.Test]
        public async Task IndexTypeIncludesUpvalueVariableDescription()
        {
            ScriptRuntimeException ex = ScriptRuntimeException.IndexType(
                DynValue.Nil,
                "upvalue 'baz'"
            );
            await Assert
                .That(ex.Message)
                .IsEqualTo("attempt to index a nil value (upvalue 'baz')")
                .ConfigureAwait(false);
        }

        [global::TUnit.Core.Test]
        public async Task LoopInIndexReturnsStockMessage()
        {
            ScriptRuntimeException ex = ScriptRuntimeException.LoopInIndex();
            await Assert.That(ex.Message).IsEqualTo("loop in gettable");
        }

        [global::TUnit.Core.Test]
        public async Task LoopInNewIndexReturnsStockMessage()
        {
            ScriptRuntimeException ex = ScriptRuntimeException.LoopInNewIndex();
            await Assert.That(ex.Message).IsEqualTo("loop in settable");
        }

        [global::TUnit.Core.Test]
        public async Task LoopInCallReturnsStockMessage()
        {
            ScriptRuntimeException ex = ScriptRuntimeException.LoopInCall();
            await Assert.That(ex.Message).IsEqualTo("loop in call");
        }

        // ====== BadArgument factories ======

        [global::TUnit.Core.Test]
        public async Task BadArgumentNoNegativeNumbersReturnsCorrectMessage()
        {
            ScriptRuntimeException ex = ScriptRuntimeException.BadArgumentNoNegativeNumbers(
                1,
                "sqrt"
            );
            await Assert
                .That(ex.Message)
                .IsEqualTo("bad argument #2 to 'sqrt' (not a non-negative number in proper range)");
        }

        [global::TUnit.Core.Test]
        public async Task BadArgumentUserDataThrowsArgumentNullExceptionForNullExpectedType()
        {
            await Assert
                .That(() =>
                    ScriptRuntimeException.BadArgumentUserData(0, "test", null, "obj", false)
                )
                .ThrowsExactly<ArgumentNullException>()
                .ConfigureAwait(false);
        }

        [global::TUnit.Core.Test]
        public async Task BadArgumentUserDataHandlesNullObjectCorrectly()
        {
            ScriptRuntimeException ex = ScriptRuntimeException.BadArgumentUserData(
                0,
                "test",
                typeof(string),
                null,
                false
            );
            await Assert.That(ex.Message).Contains("got null").ConfigureAwait(false);
        }

        [global::TUnit.Core.Test]
        public async Task BadArgumentUserDataShowsAllowNilPrefix()
        {
            ScriptRuntimeException ex = ScriptRuntimeException.BadArgumentUserData(
                0,
                "test",
                typeof(string),
                42,
                true
            );
            await Assert.That(ex.Message).Contains("nil or ").ConfigureAwait(false);
        }

        // ====== ConvertObjectFailed factories ======

        [global::TUnit.Core.Test]
        public async Task ConvertObjectFailedThrowsArgumentNullExceptionForNullObject()
        {
            await Assert
                .That(() => ScriptRuntimeException.ConvertObjectFailed((object)null))
                .ThrowsExactly<ArgumentNullException>()
                .ConfigureAwait(false);
        }

        [global::TUnit.Core.Test]
        public async Task ConvertObjectFailedWithTypeThrowsArgumentNullExceptionForNullType()
        {
            await Assert
                .That(() => ScriptRuntimeException.ConvertObjectFailed(DataType.String, null))
                .ThrowsExactly<ArgumentNullException>()
                .ConfigureAwait(false);
        }

        [global::TUnit.Core.Test]
        public async Task ConvertObjectFailedWithTypeConstraintReturnsCorrectMessage()
        {
            ScriptRuntimeException ex = ScriptRuntimeException.ConvertObjectFailed(
                DataType.Table,
                typeof(string)
            );
            await Assert
                .That(ex.Message)
                .IsEqualTo("cannot convert a table to a clr type System.String");
        }

        // ====== UserDataArgumentTypeMismatch ======

        [global::TUnit.Core.Test]
        public async Task UserDataArgumentTypeMismatchThrowsArgumentNullExceptionForNullType()
        {
            await Assert
                .That(() =>
                    ScriptRuntimeException.UserDataArgumentTypeMismatch(DataType.Table, null)
                )
                .ThrowsExactly<ArgumentNullException>()
                .ConfigureAwait(false);
        }

        // ====== AttemptToCallNonFunc ======

        [global::TUnit.Core.Test]
        public async Task AttemptToCallNonFuncReturnsMessageWithoutDebugText()
        {
            ScriptRuntimeException ex = ScriptRuntimeException.AttemptToCallNonFunc(
                DataType.Boolean
            );
            await Assert.That(ex.Message).IsEqualTo("attempt to call a boolean value");
        }

        [global::TUnit.Core.Test]
        public async Task AttemptToCallNonFuncIncludesDebugTextWhenProvided()
        {
            ScriptRuntimeException ex = ScriptRuntimeException.AttemptToCallNonFunc(
                DataType.Table,
                "myTable"
            );
            await Assert.That(ex.Message).IsEqualTo("attempt to call a table value near 'myTable'");
        }

        // ====== AccessInstanceMemberOnStatics null guards ======

        [global::TUnit.Core.Test]
        public async Task AccessInstanceMemberOnStaticsThrowsArgumentNullForNullDescriptor()
        {
            await Assert
                .That(() => ScriptRuntimeException.AccessInstanceMemberOnStatics(null))
                .ThrowsExactly<ArgumentNullException>()
                .ConfigureAwait(false);
        }

        [global::TUnit.Core.Test]
        public async Task AccessInstanceMemberOnStaticsWithTypeDescThrowsForNullTypeDescriptor()
        {
            IMemberDescriptor descriptor = new StubMemberDescriptor("Test", isStatic: false);
            await Assert
                .That(() =>
                    ScriptRuntimeException.AccessInstanceMemberOnStatics(
                        (IUserDataDescriptor)null,
                        descriptor
                    )
                )
                .ThrowsExactly<ArgumentNullException>()
                .ConfigureAwait(false);
        }

        [global::TUnit.Core.Test]
        public async Task AccessInstanceMemberOnStaticsWithTypeDescThrowsForNullMemberDescriptor()
        {
            IUserDataDescriptor typeDescriptor = new StubUserDataDescriptor("Vector3");
            await Assert
                .That(() =>
                    ScriptRuntimeException.AccessInstanceMemberOnStatics(typeDescriptor, null)
                )
                .ThrowsExactly<ArgumentNullException>()
                .ConfigureAwait(false);
        }

        // ====== GetCoroutineStateName cache fallback ======

        [global::TUnit.Core.Test]
        public async Task CannotCloseCoroutineCoversAllKnownStates()
        {
            Dictionary<CoroutineState, string> expectedMessages = new()
            {
                { CoroutineState.Main, "attempt to close the main coroutine" },
                { CoroutineState.Running, "cannot close a running coroutine" },
                { CoroutineState.Suspended, "cannot close coroutine in state suspended" },
                { CoroutineState.NotStarted, "cannot close coroutine in state notstarted" },
                { CoroutineState.Dead, "cannot close coroutine in state dead" },
            };

            foreach (KeyValuePair<CoroutineState, string> entry in expectedMessages)
            {
                ScriptRuntimeException ex = ScriptRuntimeException.CannotCloseCoroutine(entry.Key);
                await Assert.That(ex.Message).IsEqualTo(entry.Value);
            }
        }

        // ====== Original tests ======

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
            Dictionary<int, string> cases = new()
            {
                { 0, "value must be a number" },
                { 1, "'for' initial value must be a number" },
                { 2, "'for' step must be a number" },
                { 3, "'for' limit must be a number" },
                { 42, "value must be a number" },
            };

            foreach (KeyValuePair<int, string> entry in cases)
            {
                ScriptRuntimeException exception = ScriptRuntimeException.ConvertToNumberFailed(
                    entry.Key
                );
                await Assert.That(exception.Message).IsEqualTo(entry.Value);
            }
        }

        [global::TUnit.Core.Test]
        public async Task ConvertObjectFailedIncludesClrAndLuaTypes()
        {
            ScriptRuntimeException clrException = ScriptRuntimeException.ConvertObjectFailed(
                new SampleClrType()
            );
            ScriptRuntimeException luaException = ScriptRuntimeException.ConvertObjectFailed(
                DataType.String
            );

            await Assert
                .That(clrException.Message)
                .IsEqualTo($"cannot convert clr type {typeof(SampleClrType)}");
            await Assert
                .That(luaException.Message)
                .IsEqualTo("cannot convert a string to a clr type");
        }

        [global::TUnit.Core.Test]
        public async Task UserDataArgumentTypeMismatchHighlightsTypes()
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
        public async Task CannotResumeNotSuspendedDifferentiatesStates()
        {
            Dictionary<CoroutineState, string> cases = new()
            {
                { CoroutineState.Dead, "cannot resume dead coroutine" },
                { CoroutineState.Running, "cannot resume non-suspended coroutine" },
                { CoroutineState.ForceSuspended, "cannot resume non-suspended coroutine" },
            };

            foreach (KeyValuePair<CoroutineState, string> entry in cases)
            {
                ScriptRuntimeException exception = ScriptRuntimeException.CannotResumeNotSuspended(
                    entry.Key
                );
                await Assert.That(exception.Message).IsEqualTo(entry.Value);
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
        public async Task AccessInstanceMemberOnStaticsValidatesDescriptors()
        {
            IMemberDescriptor descriptor = new StubMemberDescriptor("Length", isStatic: false);
            IUserDataDescriptor typeDescriptor = new StubUserDataDescriptor("Vector3");

            ScriptRuntimeException memberOnly =
                ScriptRuntimeException.AccessInstanceMemberOnStatics(descriptor);
            ScriptRuntimeException typed = ScriptRuntimeException.AccessInstanceMemberOnStatics(
                typeDescriptor,
                descriptor
            );

            await Assert
                .That(memberOnly.Message)
                .IsEqualTo("attempt to access instance member Length from a static userdata");
            await Assert
                .That(typed.Message)
                .IsEqualTo(
                    "attempt to access instance member Vector3.Length from a static userdata"
                );
        }

        // ====== Constructor and Rethrow coverage ======

        [global::TUnit.Core.Test]
        public async Task ConstructorFromExceptionThrowsArgumentNullForNull()
        {
            await Assert
                .That(() => new ScriptRuntimeException((Exception)null))
                .ThrowsExactly<ArgumentNullException>()
                .ConfigureAwait(false);
        }

        [global::TUnit.Core.Test]
        public async Task ConstructorFromExceptionWrapsInnerException()
        {
            InvalidOperationException inner = new("Inner error");
            ScriptRuntimeException ex = new(inner);
            await Assert.That(ex.InnerException).IsEqualTo(inner);
        }

        [global::TUnit.Core.Test]
        public async Task ConstructorFromScriptRuntimeExceptionThrowsArgumentNullForNull()
        {
            await Assert
                .That(() => new ScriptRuntimeException((ScriptRuntimeException)null))
                .ThrowsExactly<ArgumentNullException>()
                .ConfigureAwait(false);
        }

        [global::TUnit.Core.Test]
        public async Task ConstructorFromScriptRuntimeExceptionPreservesDecoratedMessage()
        {
            ScriptRuntimeException original = new("original message");
            ScriptRuntimeException clone = new(original);
            // The clone wraps the original as inner exception and uses decorated message behavior
            await Assert.That(clone.InnerException).IsEqualTo(original);
            await Assert.That(clone.DoNotDecorateMessage).IsEqualTo(true);
        }

        [global::TUnit.Core.Test]
        public async Task RethrowDoesNothingWhenRethrowExceptionNestedIsFalse()
        {
            using (
                ScriptGlobalOptionsScope.Override(options =>
                {
                    options.RethrowExceptionNested = false;
                })
            )
            {
                ScriptRuntimeException ex = new("test");
                // Should not throw - if it throws, the test will fail
                ex.Rethrow();
                // The test passes if we reach here without an exception
                await Assert.That(ex.Message).IsEqualTo("test").ConfigureAwait(false);
            }
        }

        [global::TUnit.Core.Test]
        public async Task RethrowThrowsNestedExceptionWhenRethrowExceptionNestedIsTrue()
        {
            using (
                ScriptGlobalOptionsScope.Override(options =>
                {
                    options.RethrowExceptionNested = true;
                })
            )
            {
                ScriptRuntimeException original = new("original message");
                await Assert
                    .That(() => original.Rethrow())
                    .ThrowsExactly<ScriptRuntimeException>()
                    .ConfigureAwait(false);
            }
        }

        // ====== CloseMetamethodExpected branch coverage ======

        [global::TUnit.Core.Test]
        public async Task CloseMetamethodExpectedHandlesNullValue()
        {
            ScriptRuntimeException ex = ScriptRuntimeException.CloseMetamethodExpected(null);
            await Assert.That(ex.Message).IsEqualTo("__close metamethod expected (got nil)");
        }

        [global::TUnit.Core.Test]
        public async Task CloseMetamethodExpectedHandlesNonNullValue()
        {
            DynValue value = DynValue.NewNumber(42);
            ScriptRuntimeException ex = ScriptRuntimeException.CloseMetamethodExpected(value);
            await Assert.That(ex.Message).IsEqualTo("__close metamethod expected (got number)");
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
