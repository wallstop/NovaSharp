namespace NovaSharp.Interpreter.Tests.Units
{
    using System;
    using NovaSharp.Interpreter;
    using NovaSharp.Interpreter.DataTypes;
    using NovaSharp.Interpreter.Execution;
    using NovaSharp.Interpreter.Interop.StandardDescriptors.ReflectionMemberDescriptors;
    using NUnit.Framework;

    [TestFixture]
    public sealed class ValueTypeDefaultCtorMemberDescriptorTests
    {
        [Test]
        public void ConstructorRejectsNonValueTypes()
        {
            Assert.That(
                () => new ValueTypeDefaultCtorMemberDescriptor(typeof(string)),
                Throws.ArgumentException.With.Message.Contains("valueType is not a value type")
            );
        }

        [Test]
        public void ExecuteReturnsDefaultValueForValueTypes()
        {
            UserData.RegisterType<SampleStruct>();
            ValueTypeDefaultCtorMemberDescriptor descriptor = new(typeof(SampleStruct));
            Script script = new();
            ScriptExecutionContext context = TestHelpers.CreateExecutionContext(script);

            DynValue result = descriptor.Execute(
                script,
                null,
                context,
                TestHelpers.CreateArguments()
            );

            Assert.Multiple(() =>
            {
                Assert.That(result.Type, Is.EqualTo(DataType.UserData));
                Assert.That(result.UserData.Object, Is.InstanceOf<SampleStruct>());
            });
        }

        [Test]
        public void GetValueReturnsDefaultDynValue()
        {
            UserData.RegisterType<SampleStruct>();
            ValueTypeDefaultCtorMemberDescriptor descriptor = new(typeof(SampleStruct));
            Script script = new();

            DynValue value = descriptor.GetValue(script, null);

            Assert.Multiple(() =>
            {
                Assert.That(value.Type, Is.EqualTo(DataType.UserData));
                Assert.That(value.UserData.Object, Is.InstanceOf<SampleStruct>());
            });
        }

        [Test]
        public void SetValueThrowsOnWriteAttempt()
        {
            ValueTypeDefaultCtorMemberDescriptor descriptor = new(typeof(SampleStruct));
            Script script = new();

            Assert.That(
                () => descriptor.SetValue(script, null, DynValue.Nil),
                Throws.InstanceOf<NovaSharp.Interpreter.Errors.ScriptRuntimeException>()
            );
        }

        [Test]
        public void PrepareForWiringWritesMetadata()
        {
            ValueTypeDefaultCtorMemberDescriptor descriptor = new(typeof(SampleStruct));
            Script script = new();
            Table table = new(script);

            descriptor.PrepareForWiring(table);

            Assert.Multiple(() =>
            {
                Assert.That(
                    table.RawGet("class").String,
                    Is.EqualTo(descriptor.GetType().FullName)
                );
                Assert.That(table.RawGet("type").String, Is.EqualTo(typeof(SampleStruct).FullName));
                Assert.That(table.RawGet("name").String, Is.EqualTo("__new"));
            });
        }

        private struct SampleStruct
        {
            public int Value { get; set; }
        }
    }
}
