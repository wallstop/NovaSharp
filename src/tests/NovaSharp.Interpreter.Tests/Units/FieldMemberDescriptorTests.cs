namespace NovaSharp.Interpreter.Tests.Units
{
    using System.Reflection;
    using NovaSharp.Interpreter;
    using NovaSharp.Interpreter.DataTypes;
    using NovaSharp.Interpreter.Errors;
    using NovaSharp.Interpreter.Interop;
    using NovaSharp.Interpreter.Interop.BasicDescriptors;
    using NovaSharp.Interpreter.Interop.StandardDescriptors.ReflectionMemberDescriptors;
    using NUnit.Framework;

    [TestFixture]
    public sealed class FieldMemberDescriptorTests
    {
        private sealed class SampleFields
        {
            public const int ConstValue = 7;
            public static readonly string ReadonlyValue = "fixed";
            public static int StaticValue = 1;
            public int InstanceValue;
        }

        private readonly Script _script = new();

        [Test]
        public void TryCreateIfVisibleReturnsDescriptorForPublicField()
        {
            FieldInfo fieldInfo = typeof(SampleFields).GetField(
                nameof(SampleFields.StaticValue),
                BindingFlags.Static | BindingFlags.Public
            );

            FieldMemberDescriptor descriptor = FieldMemberDescriptor.TryCreateIfVisible(
                fieldInfo,
                InteropAccessMode.Reflection
            );

            Assert.That(descriptor, Is.Not.Null);
            Assert.Multiple(() =>
            {
                Assert.That(descriptor.Name, Is.EqualTo(nameof(SampleFields.StaticValue)));
                Assert.That(descriptor.IsStatic, Is.True);
            });
        }

        [Test]
        public void MemberAccessReflectsConstAndReadonlyState()
        {
            FieldMemberDescriptor constDescriptor = new(
                typeof(SampleFields).GetField(
                    nameof(SampleFields.ConstValue),
                    BindingFlags.Static | BindingFlags.Public
                ),
                InteropAccessMode.Reflection
            );
            FieldMemberDescriptor readonlyDescriptor = new(
                typeof(SampleFields).GetField(
                    nameof(SampleFields.ReadonlyValue),
                    BindingFlags.Static | BindingFlags.Public
                ),
                InteropAccessMode.Reflection
            );
            FieldMemberDescriptor writableDescriptor = new(
                typeof(SampleFields).GetField(
                    nameof(SampleFields.StaticValue),
                    BindingFlags.Static | BindingFlags.Public
                ),
                InteropAccessMode.Reflection
            );

            Assert.Multiple(() =>
            {
                Assert.That(constDescriptor.MemberAccess, Is.EqualTo(MemberDescriptorAccess.CanRead));
                Assert.That(readonlyDescriptor.MemberAccess, Is.EqualTo(MemberDescriptorAccess.CanRead));
                Assert.That(
                    writableDescriptor.MemberAccess,
                    Is.EqualTo(MemberDescriptorAccess.CanRead | MemberDescriptorAccess.CanWrite)
                );
            });
        }

        [Test]
        public void GetValueReturnsConstFieldWithoutAllocatingInstance()
        {
            FieldMemberDescriptor descriptor = new(
                typeof(SampleFields).GetField(
                    nameof(SampleFields.ConstValue),
                    BindingFlags.Static | BindingFlags.Public
                ),
                InteropAccessMode.Reflection
            );

            DynValue value = descriptor.GetValue(_script, null);

            Assert.That(value.Type, Is.EqualTo(DataType.Number));
            Assert.That(value.Number, Is.EqualTo(SampleFields.ConstValue));
        }

        [Test]
        public void SetValueRejectsConstAndReadonlyFields()
        {
            FieldMemberDescriptor constDescriptor = new(
                typeof(SampleFields).GetField(
                    nameof(SampleFields.ConstValue),
                    BindingFlags.Static | BindingFlags.Public
                ),
                InteropAccessMode.Reflection
            );
            FieldMemberDescriptor readonlyDescriptor = new(
                typeof(SampleFields).GetField(
                    nameof(SampleFields.ReadonlyValue),
                    BindingFlags.Static | BindingFlags.Public
                ),
                InteropAccessMode.Reflection
            );

            Assert.Multiple(() =>
            {
                Assert.That(
                    () => constDescriptor.SetValue(_script, null, DynValue.NewNumber(10)),
                    Throws.TypeOf<ScriptRuntimeException>().With.Message.Contains("cannot be assigned")
                );
                Assert.That(
                    () => readonlyDescriptor.SetValue(_script, null, DynValue.NewString("new")),
                    Throws.TypeOf<ScriptRuntimeException>().With.Message.Contains("cannot be assigned")
                );
            });
        }

        [Test]
        public void SetValueConvertsNumericDynValue()
        {
            SampleFields instance = new();
            FieldMemberDescriptor descriptor = new(
                typeof(SampleFields).GetField(
                    nameof(SampleFields.InstanceValue),
                    BindingFlags.Instance | BindingFlags.Public
                ),
                InteropAccessMode.Reflection
            );

            descriptor.SetValue(_script, instance, DynValue.NewNumber(5.0));

            Assert.That(instance.InstanceValue, Is.EqualTo(5));
        }

        [Test]
        public void SetValueThrowsWhenTypeMismatchOccurs()
        {
            SampleFields instance = new();
            FieldMemberDescriptor descriptor = new(
                typeof(SampleFields).GetField(
                    nameof(SampleFields.InstanceValue),
                    BindingFlags.Instance | BindingFlags.Public
                ),
                InteropAccessMode.Reflection
            );

            Assert.That(
                () => descriptor.SetValue(_script, instance, DynValue.NewString("invalid")),
                Throws.TypeOf<ScriptRuntimeException>().With.Message.Contains("cannot convert")
            );
        }
    }
}
