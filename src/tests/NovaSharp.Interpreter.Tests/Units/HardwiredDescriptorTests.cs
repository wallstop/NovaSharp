namespace NovaSharp.Interpreter.Tests.Units
{
    using System.Collections.Generic;
    using NovaSharp.Interpreter;
    using NovaSharp.Interpreter.DataTypes;
    using NovaSharp.Interpreter.Errors;
    using NovaSharp.Interpreter.Execution;
    using NovaSharp.Interpreter.Interop.BasicDescriptors;
    using NovaSharp.Interpreter.Interop.StandardDescriptors.HardwiredDescriptors;
    using NUnit.Framework;

    [TestFixture]
    public sealed class HardwiredDescriptorTests
    {
        [Test]
        public void HardwiredMemberDescriptorReturnsConvertedValue()
        {
            Script script = new Script();
            TrackingMemberDescriptor descriptor = new TrackingMemberDescriptor(
                typeof(string),
                MemberDescriptorAccess.CanRead | MemberDescriptorAccess.CanWrite
            );
            object target = new object();

            DynValue result = descriptor.GetValue(script, target);

            Assert.That(result.Type, Is.EqualTo(DataType.String));
            Assert.That(result.String, Is.EqualTo("value-for:System.Object"));
            Assert.That(descriptor.GetCallCount, Is.EqualTo(1));
        }

        [Test]
        public void HardwiredMemberDescriptorConvertsValueOnSet()
        {
            Script script = new Script();
            TrackingMemberDescriptor descriptor = new TrackingMemberDescriptor(
                typeof(int),
                MemberDescriptorAccess.CanRead | MemberDescriptorAccess.CanWrite
            );
            object target = new object();
            DynValue payload = DynValue.NewNumber(42);

            descriptor.SetValue(script, target, payload);

            Assert.That(descriptor.SetCallCount, Is.EqualTo(1));
            Assert.That(descriptor.LastAssignedValue, Is.EqualTo(42));
        }

        [Test]
        public void HardwiredMemberDescriptorThrowsWhenAccessingInstanceAsStatic()
        {
            Script script = new Script();
            TrackingMemberDescriptor descriptor = new TrackingMemberDescriptor(
                typeof(string),
                MemberDescriptorAccess.CanRead | MemberDescriptorAccess.CanWrite,
                isStatic: false
            );

            Assert.That(
                () => descriptor.GetValue(script, null),
                Throws.InstanceOf<ScriptRuntimeException>()
            );
            Assert.That(
                () => descriptor.SetValue(script, null, DynValue.NewString("data")),
                Throws.InstanceOf<ScriptRuntimeException>()
            );
        }

        [Test]
        public void HardwiredMemberDescriptorHonoursReadOnlyAccess()
        {
            Script script = new Script();
            TrackingMemberDescriptor descriptor = new TrackingMemberDescriptor(
                typeof(string),
                MemberDescriptorAccess.CanWrite
            );

            Assert.That(
                () => descriptor.GetValue(script, new object()),
                Throws.InstanceOf<ScriptRuntimeException>()
            );
            Assert.That(descriptor.GetCallCount, Is.Zero);
        }

        [Test]
        public void HardwiredMemberDescriptorHonoursWriteOnlyAccess()
        {
            Script script = new Script();
            TrackingMemberDescriptor descriptor = new TrackingMemberDescriptor(
                typeof(string),
                MemberDescriptorAccess.CanRead
            );

            Assert.That(
                () => descriptor.SetValue(script, new object(), DynValue.NewString("data")),
                Throws.InstanceOf<ScriptRuntimeException>()
            );
            Assert.That(descriptor.SetCallCount, Is.Zero);
        }

        [Test]
        public void HardwiredMethodDescriptorBuildsParametersAndReturnsResult()
        {
            Script script = new Script();
            TrackingMethodDescriptor descriptor = new TrackingMethodDescriptor();
            object instance = "owner";
            CallbackArguments args = new(
                new List<DynValue> { DynValue.NewNumber(7) },
                isMethodCall: false
            );

            DynValue result = descriptor.Execute(script, instance, context: null, args);

            Assert.That(result.Type, Is.EqualTo(DataType.String));
            Assert.That(result.String, Is.EqualTo("owner:7:fallback:2"));
            Assert.That(descriptor.LastInvocationArgs, Has.Length.EqualTo(2));
            Assert.That(descriptor.LastInvocationArgs[0], Is.EqualTo(7));
            Assert.That(descriptor.LastInvocationArgs[1], Is.EqualTo("fallback"));
            Assert.That(descriptor.LastArgsCount, Is.EqualTo(2));
        }

        [Test]
        public void HardwiredMethodDescriptorThrowsWhenInstanceMissing()
        {
            Script script = new Script();
            TrackingMethodDescriptor descriptor = new TrackingMethodDescriptor();
            CallbackArguments args = new(
                new List<DynValue> { DynValue.NewNumber(1) },
                isMethodCall: false
            );

            Assert.That(
                () => descriptor.Execute(script, obj: null, context: null, args),
                Throws.InstanceOf<ScriptRuntimeException>()
            );
        }

        [Test]
        public void DefaultValueSingletonReturnsSameInstance()
        {
            DefaultValue first = DefaultValue.Instance;
            DefaultValue second = DefaultValue.Instance;

            Assert.That(first, Is.SameAs(second));
        }

        private sealed class TrackingMemberDescriptor : HardwiredMemberDescriptor
        {
            public TrackingMemberDescriptor(
                System.Type memberType,
                MemberDescriptorAccess access,
                bool isStatic = false
            )
                : base(memberType, "tracked", isStatic, access) { }

            public int GetCallCount { get; private set; }

            public int SetCallCount { get; private set; }

            public object LastAssignedValue { get; private set; }

            protected override object GetValueImpl(Script script, object obj)
            {
                GetCallCount += 1;
                return $"value-for:{obj}";
            }

            protected override void SetValueImpl(Script script, object obj, object value)
            {
                SetCallCount += 1;
                LastAssignedValue = value;
            }
        }

        private sealed class TrackingMethodDescriptor : HardwiredMethodMemberDescriptor
        {
            public TrackingMethodDescriptor()
            {
                Initialize(
                    "Tracked",
                    isStatic: false,
                    parameters: new[]
                    {
                        new ParameterDescriptor("value", typeof(int)),
                        new ParameterDescriptor(
                            "label",
                            typeof(string),
                            hasDefaultValue: true,
                            defaultValue: "fallback"
                        ),
                    },
                    isExtensionMethod: false
                );
            }

            public object[] LastInvocationArgs { get; private set; } = System.Array.Empty<object>();

            public int LastArgsCount { get; private set; }

            protected override object Invoke(
                Script script,
                object obj,
                object[] pars,
                int argscount
            )
            {
                LastInvocationArgs = pars;
                LastArgsCount = argscount;
                return $"{obj}:{pars[0]}:{pars[1]}:{argscount}";
            }
        }
    }
}
