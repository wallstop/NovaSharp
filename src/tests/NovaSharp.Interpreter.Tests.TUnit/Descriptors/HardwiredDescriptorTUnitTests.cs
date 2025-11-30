#pragma warning disable CA2007
namespace NovaSharp.Interpreter.Tests.TUnit.Descriptors
{
    using System;
    using System.Collections.Generic;
    using System.Threading.Tasks;
    using global::TUnit.Assertions;
    using NovaSharp.Interpreter;
    using NovaSharp.Interpreter.DataTypes;
    using NovaSharp.Interpreter.Errors;
    using NovaSharp.Interpreter.Execution;
    using NovaSharp.Interpreter.Interop.BasicDescriptors;
    using NovaSharp.Interpreter.Interop.StandardDescriptors.HardwiredDescriptors;
    using NovaSharp.Interpreter.Interop.StandardDescriptors.MemberDescriptors;

    public sealed class HardwiredDescriptorTUnitTests
    {
        [global::TUnit.Core.Test]
        public async Task HardwiredMemberDescriptorReturnsConvertedValue()
        {
            Script script = new();
            TrackingMemberDescriptor descriptor = new(
                typeof(string),
                MemberDescriptorAccess.CanRead | MemberDescriptorAccess.CanWrite
            );
            object target = new object();

            DynValue result = descriptor.GetValue(script, target);

            await Assert.That(result.Type).IsEqualTo(DataType.String);
            await Assert.That(result.String).IsEqualTo("value-for:System.Object");
            await Assert.That(descriptor.GetCallCount).IsEqualTo(1);
        }

        [global::TUnit.Core.Test]
        public async Task HardwiredMemberDescriptorConvertsValueOnSet()
        {
            Script script = new();
            TrackingMemberDescriptor descriptor = new(
                typeof(int),
                MemberDescriptorAccess.CanRead | MemberDescriptorAccess.CanWrite
            );
            DynValue payload = DynValue.NewNumber(42);

            descriptor.SetValue(script, new object(), payload);

            await Assert.That(descriptor.SetCallCount).IsEqualTo(1);
            await Assert.That(descriptor.LastAssignedValue).IsEqualTo(42);
        }

        [global::TUnit.Core.Test]
        public async Task HardwiredMemberDescriptorThrowsWhenAccessingInstanceAsStatic()
        {
            Script script = new();
            TrackingMemberDescriptor descriptor = new(
                typeof(string),
                MemberDescriptorAccess.CanRead | MemberDescriptorAccess.CanWrite,
                isStatic: false
            );

            ScriptRuntimeException readException = Assert.Throws<ScriptRuntimeException>(() =>
                descriptor.GetValue(script, null)
            );
            await Assert.That(readException).IsNotNull();

            ScriptRuntimeException writeException = Assert.Throws<ScriptRuntimeException>(() =>
                descriptor.SetValue(script, null, DynValue.NewString("data"))
            );
            await Assert.That(writeException).IsNotNull();
        }

        [global::TUnit.Core.Test]
        public async Task HardwiredMemberDescriptorHonoursReadOnlyAccess()
        {
            Script script = new();
            TrackingMemberDescriptor descriptor = new(
                typeof(string),
                MemberDescriptorAccess.CanWrite
            );

            ScriptRuntimeException exception = Assert.Throws<ScriptRuntimeException>(() =>
                descriptor.GetValue(script, new object())
            );
            await Assert.That(exception).IsNotNull();
            await Assert.That(descriptor.GetCallCount).IsEqualTo(0);
        }

        [global::TUnit.Core.Test]
        public async Task HardwiredMemberDescriptorHonoursWriteOnlyAccess()
        {
            Script script = new();
            TrackingMemberDescriptor descriptor = new(
                typeof(string),
                MemberDescriptorAccess.CanRead
            );

            ScriptRuntimeException exception = Assert.Throws<ScriptRuntimeException>(() =>
                descriptor.SetValue(script, new object(), DynValue.NewString("data"))
            );
            await Assert.That(exception).IsNotNull();
            await Assert.That(descriptor.SetCallCount).IsEqualTo(0);
        }

        [global::TUnit.Core.Test]
        public async Task HardwiredMethodDescriptorBuildsParametersAndReturnsResult()
        {
            Script script = new();
            TrackingMethodDescriptor descriptor = new();
            object instance = "owner";
            CallbackArguments args = new(
                new List<DynValue> { DynValue.NewNumber(7) },
                isMethodCall: false
            );

            DynValue result = descriptor.Execute(script, instance, context: null, args);

            await Assert.That(result.Type).IsEqualTo(DataType.String);
            await Assert.That(result.String).IsEqualTo("owner:7:fallback:2");
            await Assert.That(descriptor.LastInvocationArgs.Length).IsEqualTo(2);
            await Assert.That(descriptor.LastInvocationArgs[0]).IsEqualTo(7);
            await Assert.That(descriptor.LastInvocationArgs[1]).IsEqualTo("fallback");
            await Assert.That(descriptor.LastArgsCount).IsEqualTo(2);
        }

        [global::TUnit.Core.Test]
        public async Task HardwiredMethodDescriptorThrowsWhenInstanceMissing()
        {
            Script script = new();
            TrackingMethodDescriptor descriptor = new();
            CallbackArguments args = new(
                new List<DynValue> { DynValue.NewNumber(1) },
                isMethodCall: false
            );

            ScriptRuntimeException exception = Assert.Throws<ScriptRuntimeException>(() =>
                descriptor.Execute(script, obj: null, context: null, args)
            );
            await Assert.That(exception).IsNotNull();
        }

        [global::TUnit.Core.Test]
        public async Task DefaultValueSingletonReturnsSameInstance()
        {
            DefaultValue first = DefaultValue.Instance;
            DefaultValue second = DefaultValue.Instance;

            await Assert.That(first).IsSameReferenceAs(second);
        }

        [global::TUnit.Core.Test]
        public async Task HardwiredMemberDescriptorDefaultGetImplementationThrowsInvalidOperationException()
        {
            Script script = new();
            DefaultingMemberDescriptor descriptor = new(DefaultingMemberDescriptor.Mode.Read);

            InvalidOperationException exception = Assert.Throws<InvalidOperationException>(() =>
                descriptor.GetValue(script, new object())
            );
            await Assert
                .That(exception.Message)
                .Contains("GetValue on write-only hardwired descriptor defaulting");
        }

        [global::TUnit.Core.Test]
        public async Task HardwiredMemberDescriptorDefaultSetImplementationThrowsInvalidOperationException()
        {
            Script script = new();
            DefaultingMemberDescriptor descriptor = new(DefaultingMemberDescriptor.Mode.Write);

            InvalidOperationException exception = Assert.Throws<InvalidOperationException>(() =>
                descriptor.SetValue(script, new object(), DynValue.NewNumber(1d))
            );
            await Assert
                .That(exception.Message)
                .Contains("SetValue on read-only hardwired descriptor defaulting");
        }

        [global::TUnit.Core.Test]
        public async Task HardwiredMemberDescriptorThrowsWhenValueArgumentIsNull()
        {
            Script script = new();
            TrackingMemberDescriptor descriptor = new(typeof(int), MemberDescriptorAccess.CanWrite);

            ArgumentNullException exception = Assert.Throws<ArgumentNullException>(() =>
                descriptor.SetValue(script, new object(), value: null)
            );
            await Assert.That(exception.ParamName).IsEqualTo("value");
        }

        [global::TUnit.Core.Test]
        public async Task HardwiredUserDataDescriptorCtorThrowsWhenTypeIsNull()
        {
            ArgumentNullException exception = Assert.Throws<ArgumentNullException>(() =>
            {
                _ = new StubHardwiredUserDataDescriptor(null);
            });
            await Assert.That(exception.ParamName).IsEqualTo("t");
        }

        [global::TUnit.Core.Test]
        public async Task DynValueMemberDescriptorReturnsStoredDynValue()
        {
            DynValueMemberDescriptor descriptor = new("constant", DynValue.NewNumber(123));

            DynValue result = descriptor.GetValue(new Script(), obj: null);

            await Assert.That(descriptor.IsStatic).IsTrue();
            await Assert.That(descriptor.Name).IsEqualTo("constant");
            await Assert.That(descriptor.MemberAccess).IsEqualTo(MemberDescriptorAccess.CanRead);
            await Assert.That(result.Type).IsEqualTo(DataType.Number);
            await Assert.That(result.Number).IsEqualTo(123d);
        }

        [global::TUnit.Core.Test]
        public async Task DynValueMemberDescriptorMarksClrFunctionExecutable()
        {
            DynValueMemberDescriptor descriptor = new(
                "callback",
                DynValue.NewCallback((_, _) => DynValue.NewString("ok"), "cb")
            );

            await Assert
                .That(descriptor.MemberAccess)
                .IsEqualTo(MemberDescriptorAccess.CanRead | MemberDescriptorAccess.CanExecute);
        }

        [global::TUnit.Core.Test]
        public async Task DynValueMemberDescriptorSetValueThrows()
        {
            DynValueMemberDescriptor descriptor = new("number", DynValue.NewNumber(1));

            ScriptRuntimeException exception = Assert.Throws<ScriptRuntimeException>(() =>
                descriptor.SetValue(new Script(), obj: null, DynValue.NewNumber(2))
            );
            await Assert.That(exception).IsNotNull();
        }

        [global::TUnit.Core.Test]
        public async Task DynValueMemberDescriptorPrepareForWiringSerializesPrimitive()
        {
            DynValueMemberDescriptor descriptor = new("count", DynValue.NewNumber(7));
            Table wiring = new(null);

            descriptor.PrepareForWiring(wiring);

            await Assert.That(wiring.Get("class").String).Contains("DynValueMemberDescriptor");
            await Assert.That(wiring.Get("name").String).IsEqualTo("count");
            await Assert.That(wiring.Get("value").Number).IsEqualTo(7d);
            await Assert.That(wiring.Get("error").IsNil()).IsTrue();
        }

        [global::TUnit.Core.Test]
        public async Task DynValueMemberDescriptorPrepareForWiringHandlesNonPrimeTable()
        {
            Script script = new();
            Table table = new(script);
            table.Set(1, DynValue.NewString("inner"));
            DynValueMemberDescriptor descriptor = new("table", DynValue.NewTable(table));
            Table wiring = new(null);

            descriptor.PrepareForWiring(wiring);

            await Assert.That(wiring.Get("error").String).Contains("non-prime table");
            await Assert.That(wiring.Get("value").IsNil()).IsTrue();
        }

        [global::TUnit.Core.Test]
        public async Task DynValueMemberDescriptorPrepareForWiringHandlesStaticUserData()
        {
            if (!UserData.IsTypeRegistered<DummyStaticUserData>())
            {
                UserData.RegisterType<DummyStaticUserData>();
            }

            DynValueMemberDescriptor descriptor = new(
                "userdata",
                UserData.CreateStatic<DummyStaticUserData>()
            );
            Table wiring = new(null);

            descriptor.PrepareForWiring(wiring);

            await Assert.That(wiring.Get("type").String).IsEqualTo("userdata");
            await Assert
                .That(wiring.Get("staticType").String)
                .IsEqualTo(typeof(DummyStaticUserData).FullName);
            await Assert.That(wiring.Get("visibility").String).IsEqualTo("internal");
        }

        [global::TUnit.Core.Test]
        public async Task DynValueMemberDescriptorPrepareForWiringHandlesInstanceUserDataError()
        {
            if (!UserData.IsTypeRegistered<DummyInstanceUserData>())
            {
                UserData.RegisterType<DummyInstanceUserData>();
            }

            DynValue instance = UserData.Create(new DummyInstanceUserData());
            DynValueMemberDescriptor descriptor = new("userdata", instance);
            Table wiring = new(null);

            descriptor.PrepareForWiring(wiring);

            await Assert.That(wiring.Get("error").String).Contains("non-static userdata");
            await Assert.That(wiring.Get("type").IsNil()).IsTrue();
        }

        [global::TUnit.Core.Test]
        public async Task DynValueMemberDescriptorPrepareForWiringHandlesUnsupportedTypes()
        {
            Script script = new();
            DynValue closure = script.DoString("return function() return 1 end");
            DynValueMemberDescriptor descriptor = new("closure", closure);
            Table wiring = new(null);

            descriptor.PrepareForWiring(wiring);

            await Assert.That(wiring.Get("error").String).Contains("value members not supported");
        }

        [global::TUnit.Core.Test]
        public async Task DynValueMemberDescriptorSerializedConstructorLoadsValue()
        {
            SerializedDynValueDescriptor descriptor = new("serialized", "${ 'payload' }");

            await Assert.That(descriptor.Name).IsEqualTo("serialized");
            await Assert.That(descriptor.MemberAccess).IsEqualTo(MemberDescriptorAccess.CanRead);
            await Assert.That(descriptor.Value.String).IsEqualTo("payload");
        }

        [global::TUnit.Core.Test]
        public async Task DynValueMemberDescriptorNameOnlyConstructorInitializesMetadata()
        {
            NullValueDescriptor descriptor = new("unset");

            await Assert.That(descriptor.Name).IsEqualTo("unset");
            await Assert.That(descriptor.MemberAccess).IsEqualTo(MemberDescriptorAccess.CanRead);
            await Assert.That(descriptor.Value).IsNull();
        }

        [global::TUnit.Core.Test]
        public async Task DynValueMemberDescriptorPrepareForWiringAllowsPrimeTable()
        {
            Table prime = new(null);
            prime.Set(1, DynValue.NewString("inner"));
            DynValueMemberDescriptor descriptor = new("table", DynValue.NewTable(prime));
            Table wiring = new(null);

            descriptor.PrepareForWiring(wiring);

            await Assert.That(wiring.Get("value").Type).IsEqualTo(DataType.Table);
            await Assert.That(wiring.Get("error").IsNil()).IsTrue();
        }

        private sealed class TrackingMemberDescriptor : HardwiredMemberDescriptor
        {
            public TrackingMemberDescriptor(
                Type memberType,
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

            public object[] LastInvocationArgs { get; private set; } = Array.Empty<object>();

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

        internal sealed class DummyStaticUserData
        {
            internal static readonly DummyStaticUserData Instance = new();
        }

        private sealed class DummyInstanceUserData { }

        private sealed class StubHardwiredUserDataDescriptor : HardwiredUserDataDescriptor
        {
            public StubHardwiredUserDataDescriptor(Type type)
                : base(type) { }
        }
    }
}
#pragma warning restore CA2007
