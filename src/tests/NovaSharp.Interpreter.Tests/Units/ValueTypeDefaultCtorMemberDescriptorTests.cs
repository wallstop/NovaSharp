namespace NovaSharp.Interpreter.Tests.Units
{
    using System;
    using System.Collections.Generic;
    using NovaSharp.Interpreter;
    using NovaSharp.Interpreter.DataTypes;
    using NovaSharp.Interpreter.Errors;
    using NovaSharp.Interpreter.Execution;
    using NovaSharp.Interpreter.Interop;
    using NovaSharp.Interpreter.Interop.BasicDescriptors;
    using NovaSharp.Interpreter.Interop.StandardDescriptors.ReflectionMemberDescriptors;
    using NUnit.Framework;

    [TestFixture]
    public sealed class ValueTypeDefaultCtorMemberDescriptorTests
    {
        [Test]
        public void ConstructorRejectsReferenceTypes()
        {
            Assert.That(
                () => new ValueTypeDefaultCtorMemberDescriptor(typeof(string)),
                Throws
                    .TypeOf<ArgumentException>()
                    .With.Message.Contains("valueType is not a value type")
            );
        }

        [Test]
        public void ExecuteReturnsDefaultStructValue()
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

            Assert.Multiple(() =>
            {
                Assert.That(result.Type, Is.EqualTo(DataType.UserData));
                Assert.That(result.UserData.Object, Is.InstanceOf<SampleStruct>());
                Assert.That(((SampleStruct)result.UserData.Object).Value, Is.Zero);
            });
        }

        [Test]
        public void GetValueReturnsDefaultStructValue()
        {
            RegisterSampleStruct();
            Script script = new();
            ValueTypeDefaultCtorMemberDescriptor descriptor = new(typeof(SampleStruct));

            DynValue result = descriptor.GetValue(script, obj: null);

            Assert.That(result.Type, Is.EqualTo(DataType.UserData));
            Assert.That(result.UserData.Object, Is.InstanceOf<SampleStruct>());
        }

        [Test]
        public void SetValueThrows()
        {
            ValueTypeDefaultCtorMemberDescriptor descriptor = new(typeof(SampleStruct));

            Assert.That(
                () => descriptor.SetValue(new Script(), null, DynValue.NewNumber(1)),
                Throws.TypeOf<ScriptRuntimeException>().With.Message.Contains("cannot be assigned")
            );
        }

        [Test]
        public void PrepareForWiringCapturesDescriptorMetadata()
        {
            ValueTypeDefaultCtorMemberDescriptor descriptor = new(typeof(SampleStruct));
            Table wiring = new(null);

            descriptor.PrepareForWiring(wiring);

            Assert.Multiple(() =>
            {
                Assert.That(
                    wiring.Get("class").String,
                    Does.Contain(nameof(ValueTypeDefaultCtorMemberDescriptor))
                );
                Assert.That(wiring.Get("type").String, Is.EqualTo(typeof(SampleStruct).FullName));
                Assert.That(wiring.Get("name").String, Is.EqualTo("__new"));
            });
        }

        [Test]
        public void DescriptorMetadataExposesDefaultValues()
        {
            ValueTypeDefaultCtorMemberDescriptor descriptor = new(typeof(SampleStruct));

            Assert.Multiple(() =>
            {
                Assert.That(descriptor.Parameters, Is.Empty);
                Assert.That(descriptor.ExtensionMethodType, Is.Null);
                Assert.That(descriptor.VarArgsArrayType, Is.Null);
                Assert.That(descriptor.VarArgsElementType, Is.Null);
                Assert.That(descriptor.SortDiscriminant, Is.EqualTo("@.ctor"));
                Assert.That(
                    descriptor.MemberAccess,
                    Is.EqualTo(MemberDescriptorAccess.CanRead | MemberDescriptorAccess.CanExecute)
                );
            });
        }

        private static void RegisterSampleStruct()
        {
            if (!UserData.IsTypeRegistered<SampleStruct>())
            {
                UserData.RegisterType<SampleStruct>();
            }
        }

        private struct SampleStruct
        {
            public int Value { get; set; }
        }
    }
}
