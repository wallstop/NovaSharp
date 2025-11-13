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
                Assert.That(ScriptRuntimeException.TableIndexIsNil().Message, Is.EqualTo("table index is nil"));
                Assert.That(ScriptRuntimeException.TableIndexIsNaN().Message, Is.EqualTo("table index is NaN"));
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
            ScriptRuntimeException exception = ScriptRuntimeException.ConvertObjectFailed(new SampleClrType());

            Assert.That(exception.Message, Is.EqualTo($"cannot convert clr type {typeof(SampleClrType)}"));
        }

        [Test]
        public void ConvertObjectFailedIncludesLuaTypeName()
        {
            ScriptRuntimeException exception = ScriptRuntimeException.ConvertObjectFailed(DataType.String);

            Assert.That(exception.Message, Is.EqualTo("cannot convert a string to a clr type"));
        }

        [Test]
        public void ConvertObjectFailedIncludesExpectedClrType()
        {
            ScriptRuntimeException exception = ScriptRuntimeException.ConvertObjectFailed(DataType.Boolean, typeof(Guid));

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
            ScriptRuntimeException exception = ScriptRuntimeException.UserDataMissingField("Widget", "length");

            Assert.That(
                exception.Message,
                Is.EqualTo("cannot access field length of userdata<Widget>")
            );
        }

        [TestCase(CoroutineState.Dead, "cannot resume dead coroutine")]
        [TestCase(CoroutineState.Running, "cannot resume non-suspended coroutine")]
        [TestCase(CoroutineState.ForceSuspended, "cannot resume non-suspended coroutine")]
        public void CannotResumeNotSuspendedDifferentiatesStates(CoroutineState state, string expected)
        {
            ScriptRuntimeException exception = ScriptRuntimeException.CannotResumeNotSuspended(state);

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
                Is.EqualTo("attempt to access instance member Vector3.Length from a static userdata")
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

                ScriptRuntimeException rethrown = Assert.Throws<ScriptRuntimeException>(
                    () => exception.Rethrow()
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

            public DynValue GetValue(Script script, object obj) => throw new NotSupportedException();

            public void SetValue(Script script, object obj, DynValue value) => throw new NotSupportedException();
        }

        private sealed class StubUserDataDescriptor : IUserDataDescriptor
        {
            public StubUserDataDescriptor(string name)
            {
                Name = name;
            }

            public string Name { get; }

            public Type Type => typeof(object);

            public DynValue Index(Script script, object obj, DynValue index, bool isDirectIndexing) =>
                throw new NotSupportedException();

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

            public bool IsTypeCompatible(Type type, object obj) => throw new NotSupportedException();
        }
    }
}
