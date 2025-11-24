namespace NovaSharp.Interpreter.Tests.Units
{
    using System;
    using System.Collections.Generic;
    using NovaSharp.Interpreter;
    using NovaSharp.Interpreter.DataTypes;
    using NovaSharp.Interpreter.Errors;
    using NovaSharp.Interpreter.Execution;
    using NovaSharp.Interpreter.Interop.BasicDescriptors;
    using NovaSharp.Interpreter.Interop.StandardDescriptors.HardwiredDescriptors;
    using NovaSharp.Interpreter.Interop.StandardDescriptors.MemberDescriptors;
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

        [Test]
        public void HardwiredMemberDescriptorDefaultGetImplementationThrowsInvalidOperationException()
        {
            Script script = new Script();
            DefaultingMemberDescriptor descriptor = new(DefaultingMemberDescriptor.Mode.Read);

            InvalidOperationException ex = Assert.Throws<InvalidOperationException>(() =>
                descriptor.GetValue(script, new object())
            );

            Assert.That(
                ex.Message,
                Does.Contain("GetValue on write-only hardwired descriptor defaulting")
            );
        }

        [Test]
        public void HardwiredMemberDescriptorDefaultSetImplementationThrowsInvalidOperationException()
        {
            Script script = new Script();
            DefaultingMemberDescriptor descriptor = new(DefaultingMemberDescriptor.Mode.Write);

            InvalidOperationException ex = Assert.Throws<InvalidOperationException>(() =>
                descriptor.SetValue(script, new object(), DynValue.NewNumber(1d))
            );

            Assert.That(
                ex.Message,
                Does.Contain("SetValue on read-only hardwired descriptor defaulting")
            );
        }

        [Test]
        public void DynValueMemberDescriptorReturnsStoredDynValue()
        {
            DynValueMemberDescriptor descriptor = new("constant", DynValue.NewNumber(123));

            DynValue result = descriptor.GetValue(new Script(), obj: null);

            Assert.Multiple(() =>
            {
                Assert.That(descriptor.IsStatic, Is.True);
                Assert.That(descriptor.Name, Is.EqualTo("constant"));
                Assert.That(descriptor.MemberAccess, Is.EqualTo(MemberDescriptorAccess.CanRead));
                Assert.That(result.Type, Is.EqualTo(DataType.Number));
                Assert.That(result.Number, Is.EqualTo(123d));
            });
        }

        [Test]
        public void DynValueMemberDescriptorMarksClrFunctionExecutable()
        {
            DynValueMemberDescriptor descriptor = new(
                "callback",
                DynValue.NewCallback((context, args) => DynValue.NewString("ok"), "cb")
            );

            Assert.That(
                descriptor.MemberAccess,
                Is.EqualTo(MemberDescriptorAccess.CanRead | MemberDescriptorAccess.CanExecute)
            );
        }

        [Test]
        public void DynValueMemberDescriptorSetValueThrows()
        {
            DynValueMemberDescriptor descriptor = new("number", DynValue.NewNumber(1));

            Assert.That(
                () => descriptor.SetValue(new Script(), obj: null, DynValue.NewNumber(2)),
                Throws.InstanceOf<ScriptRuntimeException>()
            );
        }

        [Test]
        public void DynValueMemberDescriptorPrepareForWiringSerializesPrimitive()
        {
            DynValueMemberDescriptor descriptor = new("count", DynValue.NewNumber(7));
            Table wiring = new(null);

            descriptor.PrepareForWiring(wiring);

            Assert.Multiple(() =>
            {
                Assert.That(wiring.Get("class").String, Does.Contain("DynValueMemberDescriptor"));
                Assert.That(wiring.Get("name").String, Is.EqualTo("count"));
                Assert.That(wiring.Get("value").Number, Is.EqualTo(7d));
                Assert.That(wiring.Get("error").IsNil(), Is.True);
            });
        }

        [Test]
        public void DynValueMemberDescriptorPrepareForWiringHandlesNonPrimeTable()
        {
            Script script = new();
            Table table = new Table(script);
            table.Set(1, DynValue.NewString("inner"));
            DynValueMemberDescriptor descriptor = new("table", DynValue.NewTable(table));
            Table wiring = new(null);

            descriptor.PrepareForWiring(wiring);

            Assert.Multiple(() =>
            {
                Assert.That(wiring.Get("error").String, Does.Contain("non-prime table"));
                Assert.That(wiring.Get("value").IsNil(), Is.True);
            });
        }

        [Test]
        public void DynValueMemberDescriptorPrepareForWiringHandlesStaticUserData()
        {
            if (!UserData.IsTypeRegistered(typeof(DummyStaticUserData)))
            {
                UserData.RegisterType<DummyStaticUserData>();
            }
            DynValueMemberDescriptor descriptor = new(
                "userdata",
                UserData.CreateStatic(typeof(DummyStaticUserData))
            );
            Table wiring = new(null);

            descriptor.PrepareForWiring(wiring);

            Assert.Multiple(() =>
            {
                Assert.That(wiring.Get("type").String, Is.EqualTo("userdata"));
                Assert.That(
                    wiring.Get("staticType").String,
                    Is.EqualTo(typeof(DummyStaticUserData).FullName)
                );
                Assert.That(wiring.Get("visibility").String, Is.EqualTo("internal"));
            });
        }

        [Test]
        public void DynValueMemberDescriptorPrepareForWiringHandlesInstanceUserDataError()
        {
            if (!UserData.IsTypeRegistered(typeof(DummyInstanceUserData)))
            {
                UserData.RegisterType<DummyInstanceUserData>();
            }
            DynValue instance = UserData.Create(new DummyInstanceUserData());
            DynValueMemberDescriptor descriptor = new("userdata", instance);
            Table wiring = new(null);

            descriptor.PrepareForWiring(wiring);

            Assert.Multiple(() =>
            {
                Assert.That(wiring.Get("error").String, Does.Contain("non-static userdata"));
                Assert.That(wiring.Get("type").IsNil(), Is.True);
            });
        }

        [Test]
        public void DynValueMemberDescriptorPrepareForWiringHandlesUnsupportedTypes()
        {
            Script script = new();
            DynValue closure = script.DoString("return function() return 1 end");
            DynValueMemberDescriptor descriptor = new("closure", closure);
            Table wiring = new(null);

            descriptor.PrepareForWiring(wiring);

            Assert.That(wiring.Get("error").String, Does.Contain("value members not supported"));
        }

        [Test]
        public void DynValueMemberDescriptorSerializedConstructorLoadsValue()
        {
            SerializedDynValueDescriptor descriptor = new("serialized", "${ 'payload' }");

            Assert.Multiple(() =>
            {
                Assert.That(descriptor.Name, Is.EqualTo("serialized"));
                Assert.That(descriptor.MemberAccess, Is.EqualTo(MemberDescriptorAccess.CanRead));
                Assert.That(descriptor.Value.String, Is.EqualTo("payload"));
            });
        }

        [Test]
        public void DynValueMemberDescriptorNameOnlyConstructorInitializesMetadata()
        {
            NullValueDescriptor descriptor = new("unset");

            Assert.Multiple(() =>
            {
                Assert.That(descriptor.Name, Is.EqualTo("unset"));
                Assert.That(descriptor.MemberAccess, Is.EqualTo(MemberDescriptorAccess.CanRead));
                Assert.That(descriptor.Value, Is.Null);
            });
        }

        [Test]
        public void DynValueMemberDescriptorPrepareForWiringAllowsPrimeTable()
        {
            Table prime = new(null);
            prime.Set(1, DynValue.NewString("inner"));
            DynValueMemberDescriptor descriptor = new("table", DynValue.NewTable(prime));
            Table wiring = new(null);

            descriptor.PrepareForWiring(wiring);

            Assert.Multiple(() =>
            {
                Assert.That(wiring.Get("value").Type, Is.EqualTo(DataType.Table));
                Assert.That(wiring.Get("error").IsNil(), Is.True);
            });
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

            protected override object GetValueCore(Script script, object obj)
            {
                GetCallCount += 1;
                return $"value-for:{obj}";
            }

            protected override void SetValueCore(Script script, object obj, object value)
            {
                SetCallCount += 1;
                LastAssignedValue = value;
            }
        }

        private sealed class DefaultingMemberDescriptor : HardwiredMemberDescriptor
        {
            public enum Mode
            {
                Read,
                Write,
            }

            public DefaultingMemberDescriptor(Mode mode)
                : base(typeof(object), "defaulting", isStatic: false, GetAccess(mode)) { }

            private static MemberDescriptorAccess GetAccess(Mode mode)
            {
                return mode == Mode.Read
                    ? MemberDescriptorAccess.CanRead
                    : MemberDescriptorAccess.CanWrite;
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

        private sealed class SerializedDynValueDescriptor : DynValueMemberDescriptor
        {
            public SerializedDynValueDescriptor(string name, string serialized)
                : base(name, serialized) { }

            public new DynValue Value => base.Value;
        }

        private sealed class NullValueDescriptor : DynValueMemberDescriptor
        {
            public NullValueDescriptor(string name)
                : base(name) { }
        }

        internal sealed class DummyStaticUserData { }

        private sealed class DummyInstanceUserData { }
    }
}
