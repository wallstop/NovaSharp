#pragma warning disable CA2007
namespace NovaSharp.Interpreter.Tests.TUnit.Units
{
    using System;
    using System.Collections.Generic;
    using System.Threading.Tasks;
    using global::TUnit.Assertions;
    using NovaSharp.Interpreter;
    using NovaSharp.Interpreter.DataTypes;
    using NovaSharp.Interpreter.Errors;
    using NovaSharp.Interpreter.Execution;
    using NovaSharp.Interpreter.Interop;
    using NovaSharp.Interpreter.Interop.BasicDescriptors;
    using NovaSharp.Interpreter.Interop.StandardDescriptors.ReflectionMemberDescriptors;

    [UserDataIsolation]
    public sealed class ValueTypeDefaultCtorMemberDescriptorTUnitTests
    {
        [global::TUnit.Core.Test]
        public async Task ConstructorRejectsReferenceTypes()
        {
            ArgumentException exception = Assert.Throws<ArgumentException>(() =>
                CreateDescriptor(typeof(string))
            );

            await Assert.That(exception.Message).Contains("valueType is not a value type");
        }

        [global::TUnit.Core.Test]
        public async Task ExecuteReturnsDefaultStructValue()
        {
            RegisterSampleStruct();
            Script script = new();
            ValueTypeDefaultCtorMemberDescriptor descriptor = new(typeof(SampleStruct));

            DynValue result = descriptor.Execute(
                script,
                obj: null,
                context: null,
                args: new CallbackArguments(new List<DynValue>(), isMethodCall: false)
            );

            await Assert.That(result.Type).IsEqualTo(DataType.UserData);
            await Assert.That(result.UserData.Object).IsTypeOf<SampleStruct>();
            SampleStruct payload = (SampleStruct)result.UserData.Object;
            await Assert.That(payload.Value).IsEqualTo(0);
        }

        [global::TUnit.Core.Test]
        public async Task GetValueReturnsDefaultStructValue()
        {
            RegisterSampleStruct();
            Script script = new();
            ValueTypeDefaultCtorMemberDescriptor descriptor = new(typeof(SampleStruct));

            DynValue result = descriptor.GetValue(script, obj: null);

            await Assert.That(result.Type).IsEqualTo(DataType.UserData);
            await Assert.That(result.UserData.Object).IsTypeOf<SampleStruct>();
        }

        [global::TUnit.Core.Test]
        public async Task SetValueThrows()
        {
            ValueTypeDefaultCtorMemberDescriptor descriptor = new(typeof(SampleStruct));

            ScriptRuntimeException exception = Assert.Throws<ScriptRuntimeException>(() =>
                descriptor.SetValue(new Script(), null, DynValue.NewNumber(1))
            );

            await Assert.That(exception.Message).Contains("cannot be assigned");
        }

        [global::TUnit.Core.Test]
        public async Task PrepareForWiringCapturesDescriptorMetadata()
        {
            ValueTypeDefaultCtorMemberDescriptor descriptor = new(typeof(SampleStruct));
            Table wiring = new(null);

            descriptor.PrepareForWiring(wiring);

            await Assert
                .That(wiring.Get("class").String)
                .Contains(nameof(ValueTypeDefaultCtorMemberDescriptor));
            await Assert.That(wiring.Get("type").String).IsEqualTo(typeof(SampleStruct).FullName);
            await Assert.That(wiring.Get("name").String).IsEqualTo("__new");
        }

        [global::TUnit.Core.Test]
        public async Task PrepareForWiringThrowsWhenTableIsNull()
        {
            ValueTypeDefaultCtorMemberDescriptor descriptor = new(typeof(SampleStruct));

            ArgumentNullException exception = Assert.Throws<ArgumentNullException>(() =>
                descriptor.PrepareForWiring(null)
            );

            await Assert.That(exception.ParamName).IsEqualTo("t");
        }

        [global::TUnit.Core.Test]
        public async Task DescriptorMetadataExposesDefaultValues()
        {
            ValueTypeDefaultCtorMemberDescriptor descriptor = new(typeof(SampleStruct));

            await Assert.That(descriptor.Parameters.Count).IsEqualTo(0);
            await Assert.That(descriptor.ExtensionMethodType).IsNull();
            await Assert.That(descriptor.VarArgsArrayType).IsNull();
            await Assert.That(descriptor.VarArgsElementType).IsNull();
            await Assert.That(descriptor.SortDiscriminant).IsEqualTo("@.ctor");
            await Assert
                .That(descriptor.MemberAccess)
                .IsEqualTo(MemberDescriptorAccess.CanRead | MemberDescriptorAccess.CanExecute);
        }

        private static void RegisterSampleStruct()
        {
            if (!UserData.IsTypeRegistered<SampleStruct>())
            {
                UserData.RegisterType<SampleStruct>();
            }
        }

        private static ValueTypeDefaultCtorMemberDescriptor CreateDescriptor(Type type)
        {
            return new ValueTypeDefaultCtorMemberDescriptor(type);
        }

        private struct SampleStruct
        {
            public int Value { get; set; }
        }
    }
}
#pragma warning restore CA2007
