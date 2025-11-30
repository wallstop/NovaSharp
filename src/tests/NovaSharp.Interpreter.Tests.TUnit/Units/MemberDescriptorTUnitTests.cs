namespace NovaSharp.Interpreter.Tests.TUnit.Units
{
    using System;
    using System.Threading.Tasks;
    using global::TUnit.Assertions;
    using NovaSharp.Interpreter;
    using NovaSharp.Interpreter.DataTypes;
    using NovaSharp.Interpreter.Errors;
    using NovaSharp.Interpreter.Execution;
    using NovaSharp.Interpreter.Interop.BasicDescriptors;

    public sealed class MemberDescriptorTUnitTests
    {
        [global::TUnit.Core.Test]
        public async Task HasAllFlagsReturnsTrueOnlyWhenAllBitsPresent()
        {
            MemberDescriptorAccess access =
                MemberDescriptorAccess.CanRead | MemberDescriptorAccess.CanWrite;

            await Assert.That(access.HasAllFlags(MemberDescriptorAccess.CanRead)).IsTrue();
            await Assert.That(access.HasAllFlags(MemberDescriptorAccess.CanWrite)).IsTrue();
            await Assert
                .That(
                    access.HasAllFlags(
                        MemberDescriptorAccess.CanRead | MemberDescriptorAccess.CanWrite
                    )
                )
                .IsTrue();
            await Assert.That(access.HasAllFlags(MemberDescriptorAccess.CanExecute)).IsFalse();
        }

        [global::TUnit.Core.Test]
        public async Task CanReadWriteExecuteThrowWhenDescriptorNull()
        {
            ArgumentNullException read = Assert.Throws<ArgumentNullException>(() =>
                MemberDescriptor.CanRead(null)
            );
            ArgumentNullException write = Assert.Throws<ArgumentNullException>(() =>
                MemberDescriptor.CanWrite(null)
            );
            ArgumentNullException execute = Assert.Throws<ArgumentNullException>(() =>
                MemberDescriptor.CanExecute(null)
            );

            await Assert.That(read.ParamName).IsEqualTo("desc");
            await Assert.That(write.ParamName).IsEqualTo("desc");
            await Assert.That(execute.ParamName).IsEqualTo("desc");
        }

        [global::TUnit.Core.Test]
        public async Task CanReadWriteExecuteReflectDescriptorAccess()
        {
            StubDescriptor descriptor = new(
                name: "foo",
                isStatic: true,
                access: MemberDescriptorAccess.CanRead | MemberDescriptorAccess.CanExecute
            );

            await Assert.That(descriptor.CanRead()).IsTrue();
            await Assert.That(descriptor.CanExecute()).IsTrue();
            await Assert.That(descriptor.CanWrite()).IsFalse();
        }

        [global::TUnit.Core.Test]
        public async Task GetGetterCallbackExecutesDescriptorGetValue()
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

            await Assert.That(ReferenceEquals(result, expected)).IsTrue();
        }

        [global::TUnit.Core.Test]
        public async Task WithAccessOrNullReturnsDescriptorOnlyWhenSupported()
        {
            StubDescriptor descriptor = new(
                name: "foo",
                isStatic: true,
                access: MemberDescriptorAccess.CanRead | MemberDescriptorAccess.CanWrite
            );

            await Assert
                .That(descriptor.WithAccessOrNull(MemberDescriptorAccess.CanRead))
                .IsEqualTo(descriptor);
            await Assert
                .That(descriptor.WithAccessOrNull(MemberDescriptorAccess.CanWrite))
                .IsEqualTo(descriptor);
            await Assert
                .That(descriptor.WithAccessOrNull(MemberDescriptorAccess.CanExecute))
                .IsNull();
        }

        [global::TUnit.Core.Test]
        public async Task WithAccessOrNullReturnsNullWhenDescriptorNull()
        {
            await Assert
                .That(MemberDescriptor.WithAccessOrNull(null, MemberDescriptorAccess.CanRead))
                .IsNull();
        }

        [global::TUnit.Core.Test]
        public async Task CheckAccessThrowsWhenInstanceMemberAccessedStatically()
        {
            StubDescriptor descriptor = new(
                name: "instanceMember",
                isStatic: false,
                access: MemberDescriptorAccess.CanRead
            );

            ScriptRuntimeException exception = Assert.Throws<ScriptRuntimeException>(() =>
                descriptor.CheckAccess(MemberDescriptorAccess.CanRead, obj: null)
            );

            await Assert.That(exception.Message).Contains("instanceMember");
        }

        [global::TUnit.Core.Test]
        public async Task CheckAccessThrowsWhenPermissionMissing()
        {
            StubDescriptor descriptor = new(
                name: "writeOnly",
                isStatic: true,
                access: MemberDescriptorAccess.CanWrite
            );

            ScriptRuntimeException read = Assert.Throws<ScriptRuntimeException>(() =>
                descriptor.CheckAccess(MemberDescriptorAccess.CanRead, new object())
            );
            ScriptRuntimeException execute = Assert.Throws<ScriptRuntimeException>(() =>
                descriptor.CheckAccess(MemberDescriptorAccess.CanExecute, new object())
            );

            await Assert.That(read.Message).Contains("cannot be read");
            await Assert.That(execute.Message).Contains("cannot be called");
        }

        [global::TUnit.Core.Test]
        public void CheckAccessSucceedsWhenAccessMatches()
        {
            StubDescriptor descriptor = new(
                name: "callable",
                isStatic: true,
                access: MemberDescriptorAccess.CanExecute | MemberDescriptorAccess.CanRead
            );

            descriptor.CheckAccess(
                MemberDescriptorAccess.CanExecute | MemberDescriptorAccess.CanRead,
                new object()
            );
        }

        [global::TUnit.Core.Test]
        public async Task GetGetterCallbackThrowsWhenDescriptorNull()
        {
            Script script = new();

            ArgumentNullException exception = Assert.Throws<ArgumentNullException>(() =>
                MemberDescriptor.GetGetterCallbackAsDynValue(null, script, new object())
            );

            await Assert.That(exception.ParamName).IsEqualTo("desc");
        }

        [global::TUnit.Core.Test]
        public async Task CheckAccessThrowsWhenDescriptorNull()
        {
            ArgumentNullException exception = Assert.Throws<ArgumentNullException>(() =>
                MemberDescriptor.CheckAccess(null, MemberDescriptorAccess.CanRead, new object())
            );

            await Assert.That(exception.ParamName).IsEqualTo("desc");
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
