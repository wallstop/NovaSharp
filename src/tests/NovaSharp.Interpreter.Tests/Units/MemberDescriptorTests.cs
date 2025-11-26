namespace NovaSharp.Interpreter.Tests.Units
{
    using System;
    using NovaSharp.Interpreter;
    using NovaSharp.Interpreter.DataTypes;
    using NovaSharp.Interpreter.Errors;
    using NovaSharp.Interpreter.Execution;
    using NovaSharp.Interpreter.Interop.BasicDescriptors;
    using NUnit.Framework;

    [TestFixture]
    public sealed class MemberDescriptorTests
    {
        [Test]
        public void HasAllFlagsReturnsTrueOnlyWhenAllBitsPresent()
        {
            MemberDescriptorAccess access =
                MemberDescriptorAccess.CanRead | MemberDescriptorAccess.CanWrite;

            Assert.Multiple(() =>
            {
                Assert.That(access.HasAllFlags(MemberDescriptorAccess.CanRead), Is.True);
                Assert.That(access.HasAllFlags(MemberDescriptorAccess.CanWrite), Is.True);
                Assert.That(
                    access.HasAllFlags(
                        MemberDescriptorAccess.CanRead | MemberDescriptorAccess.CanWrite
                    ),
                    Is.True
                );
                Assert.That(access.HasAllFlags(MemberDescriptorAccess.CanExecute), Is.False);
            });
        }

        [Test]
        public void CanReadWriteExecuteThrowWhenDescriptorNull()
        {
            Assert.Multiple(() =>
            {
                Assert.That(
                    () => MemberDescriptor.CanRead(null),
                    Throws.ArgumentNullException.With.Property("ParamName").EqualTo("desc")
                );
                Assert.That(
                    () => MemberDescriptor.CanWrite(null),
                    Throws.ArgumentNullException.With.Property("ParamName").EqualTo("desc")
                );
                Assert.That(
                    () => MemberDescriptor.CanExecute(null),
                    Throws.ArgumentNullException.With.Property("ParamName").EqualTo("desc")
                );
            });
        }

        [Test]
        public void CanReadWriteExecuteReflectDescriptorAccess()
        {
            StubDescriptor descriptor = new(
                name: "foo",
                isStatic: true,
                access: MemberDescriptorAccess.CanRead | MemberDescriptorAccess.CanExecute
            );

            Assert.Multiple(() =>
            {
                Assert.That(descriptor.CanRead(), Is.True);
                Assert.That(descriptor.CanExecute(), Is.True);
                Assert.That(descriptor.CanWrite(), Is.False);
            });
        }

        [Test]
        public void GetGetterCallbackExecutesDescriptorGetValue()
        {
            Script script = new();
            DynValue expected = DynValue.NewNumber(42);
            StubDescriptor descriptor = new(
                name: "answer",
                isStatic: true,
                access: MemberDescriptorAccess.CanRead,
                valueFactory: () => expected
            );

            DynValue getter = descriptor.GetGetterCallbackAsDynValue(script, obj: null);
            ScriptExecutionContext context = script.CreateDynamicExecutionContext();
            DynValue result = getter.Callback.Invoke(
                context,
                Array.Empty<DynValue>(),
                isMethodCall: false
            );

            Assert.That(result, Is.SameAs(expected));
        }

        [Test]
        public void WithAccessOrNullReturnsDescriptorOnlyWhenSupported()
        {
            StubDescriptor descriptor = new(
                name: "foo",
                isStatic: true,
                access: MemberDescriptorAccess.CanRead | MemberDescriptorAccess.CanWrite
            );

            Assert.Multiple(() =>
            {
                Assert.That(
                    descriptor.WithAccessOrNull(MemberDescriptorAccess.CanRead),
                    Is.SameAs(descriptor)
                );
                Assert.That(
                    descriptor.WithAccessOrNull(MemberDescriptorAccess.CanWrite),
                    Is.SameAs(descriptor)
                );
                Assert.That(
                    descriptor.WithAccessOrNull(MemberDescriptorAccess.CanExecute),
                    Is.Null
                );
            });
        }

        [Test]
        public void WithAccessOrNullReturnsNullWhenDescriptorNull()
        {
            Assert.That(
                MemberDescriptor.WithAccessOrNull(null, MemberDescriptorAccess.CanRead),
                Is.Null
            );
        }

        [Test]
        public void CheckAccessThrowsWhenInstanceMemberAccessedStatically()
        {
            StubDescriptor descriptor = new(
                name: "instanceMember",
                isStatic: false,
                access: MemberDescriptorAccess.CanRead
            );

            ScriptRuntimeException ex = Assert.Throws<ScriptRuntimeException>(() =>
                descriptor.CheckAccess(MemberDescriptorAccess.CanRead, obj: null)
            );
            Assert.That(ex.Message, Does.Contain("instanceMember"));
        }

        [Test]
        public void CheckAccessThrowsWhenPermissionMissing()
        {
            StubDescriptor descriptor = new(
                name: "writeOnly",
                isStatic: true,
                access: MemberDescriptorAccess.CanWrite
            );

            Assert.Multiple(() =>
            {
                Assert.That(
                    () => descriptor.CheckAccess(MemberDescriptorAccess.CanRead, new object()),
                    Throws.TypeOf<ScriptRuntimeException>().With.Message.Contain("cannot be read")
                );
                Assert.That(
                    () => descriptor.CheckAccess(MemberDescriptorAccess.CanExecute, new object()),
                    Throws.TypeOf<ScriptRuntimeException>().With.Message.Contain("cannot be called")
                );
            });
        }

        [Test]
        public void CheckAccessSucceedsWhenAccessMatches()
        {
            StubDescriptor descriptor = new(
                name: "callable",
                isStatic: true,
                access: MemberDescriptorAccess.CanExecute | MemberDescriptorAccess.CanRead
            );

            Assert.DoesNotThrow(() =>
                descriptor.CheckAccess(
                    MemberDescriptorAccess.CanExecute | MemberDescriptorAccess.CanRead,
                    new object()
                )
            );
        }

        [Test]
        public void GetGetterCallbackThrowsWhenDescriptorNull()
        {
            Script script = new();
            Assert.That(
                () => MemberDescriptor.GetGetterCallbackAsDynValue(null, script, new object()),
                Throws.ArgumentNullException.With.Property("ParamName").EqualTo("desc")
            );
        }

        [Test]
        public void CheckAccessThrowsWhenDescriptorNull()
        {
            Assert.That(
                () =>
                    MemberDescriptor.CheckAccess(
                        null,
                        MemberDescriptorAccess.CanRead,
                        new object()
                    ),
                Throws.ArgumentNullException.With.Property("ParamName").EqualTo("desc")
            );
        }

        private sealed class StubDescriptor : IMemberDescriptor
        {
            private readonly Func<DynValue> _valueFactory;

            internal StubDescriptor(
                string name,
                bool isStatic,
                MemberDescriptorAccess access,
                Func<DynValue> valueFactory = null
            )
            {
                Name = name;
                IsStatic = isStatic;
                MemberAccess = access;
                _valueFactory = valueFactory ?? (() => DynValue.NewNil());
            }

            public bool IsStatic { get; }

            public string Name { get; }

            public MemberDescriptorAccess MemberAccess { get; }

            public DynValue GetValue(Script script, object obj)
            {
                return _valueFactory();
            }

            public void SetValue(Script script, object obj, DynValue value) { }
        }
    }
}
