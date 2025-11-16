namespace NovaSharp.Interpreter.Tests.Units
{
    using System;
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
        public void TryCreateIfVisibleRejectsNonPublicField()
        {
            FieldInfo fieldInfo = typeof(SampleFields).GetField(
                "_privateValue",
                BindingFlags.Instance | BindingFlags.NonPublic
            );

            FieldMemberDescriptor descriptor = FieldMemberDescriptor.TryCreateIfVisible(
                fieldInfo,
                InteropAccessMode.Reflection
            );

            Assert.That(descriptor, Is.Null);
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
                Assert.That(
                    constDescriptor.MemberAccess,
                    Is.EqualTo(MemberDescriptorAccess.CanRead)
                );
                Assert.That(
                    readonlyDescriptor.MemberAccess,
                    Is.EqualTo(MemberDescriptorAccess.CanRead)
                );
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
        public void GetValueUsingPreoptimizedGetterReturnsStaticField()
        {
            FieldMemberDescriptor descriptor = new(
                typeof(SampleFields).GetField(
                    nameof(SampleFields.StaticValue),
                    BindingFlags.Static | BindingFlags.Public
                ),
                InteropAccessMode.Preoptimized
            );

            Func<object, object> optimizedGetter = GetOptimizedGetter(descriptor);

            Assert.Multiple(() =>
            {
                Assert.That(optimizedGetter, Is.Not.Null, "Getter compiled during construction");
                Assert.That(
                    descriptor.GetValue(_script, null).Number,
                    Is.EqualTo(SampleFields.StaticValue)
                );
            });
        }

        [Test]
        public void LazyOptimizedGetterCompilesOnFirstAccess()
        {
            FieldMemberDescriptor descriptor = new(
                typeof(SampleFields).GetField(
                    nameof(SampleFields.StaticValue),
                    BindingFlags.Static | BindingFlags.Public
                ),
                InteropAccessMode.LazyOptimized
            );

            Assert.That(GetOptimizedGetter(descriptor), Is.Null, "Getter not yet compiled");

            DynValue first = descriptor.GetValue(_script, null);
            Func<object, object> optimizedGetter = GetOptimizedGetter(descriptor);
            DynValue second = descriptor.GetValue(_script, null);

            Assert.Multiple(() =>
            {
                Assert.That(first.Number, Is.EqualTo(SampleFields.StaticValue));
                Assert.That(optimizedGetter, Is.Not.Null, "Getter compiled after first access");
                Assert.That(second.Number, Is.EqualTo(SampleFields.StaticValue));
            });
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
                    Throws
                        .TypeOf<ScriptRuntimeException>()
                        .With.Message.Contains("cannot be assigned")
                );
                Assert.That(
                    () => readonlyDescriptor.SetValue(_script, null, DynValue.NewString("new")),
                    Throws
                        .TypeOf<ScriptRuntimeException>()
                        .With.Message.Contains("cannot be assigned")
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

        [Test]
        public void SetValueThrowsWhenInstanceIsMissing()
        {
            FieldMemberDescriptor descriptor = new(
                typeof(SampleFields).GetField(
                    nameof(SampleFields.InstanceValue),
                    BindingFlags.Instance | BindingFlags.Public
                ),
                InteropAccessMode.Reflection
            );

            Assert.That(
                () => descriptor.SetValue(_script, null, DynValue.NewNumber(1)),
                Throws
                    .TypeOf<ScriptRuntimeException>()
                    .With.Message.Contains("attempt to access instance member InstanceValue")
            );
        }

        [Test]
        public void PrepareForWiringPopulatesMetadataTable()
        {
            FieldMemberDescriptor descriptor = new(
                typeof(SampleFields).GetField(
                    nameof(SampleFields.StaticValue),
                    BindingFlags.Static | BindingFlags.Public
                ),
                InteropAccessMode.Reflection
            );
            Table table = new(_script);

            descriptor.PrepareForWiring(table);

            Assert.Multiple(() =>
            {
                Assert.That(table.Get("name").String, Is.EqualTo(nameof(SampleFields.StaticValue)));
                Assert.That(table.Get("static").Boolean, Is.True);
                Assert.That(table.Get("write").Boolean, Is.True);
                Assert.That(table.Get("const").Boolean, Is.False);
            });
        }

        private static Func<object, object> GetOptimizedGetter(FieldMemberDescriptor descriptor)
        {
            FieldInfo getterField = typeof(FieldMemberDescriptor).GetField(
                "_optimizedGetter",
                BindingFlags.Instance | BindingFlags.NonPublic
            );
            return (Func<object, object>)getterField.GetValue(descriptor);
        }

        private sealed class SampleFields
        {
            public const int ConstValue = 7;
            public static readonly string ReadonlyValue = "fixed";
            public static int StaticValue = 1;
            public int InstanceValue;
            private int _privateValue;
        }
    }
}
