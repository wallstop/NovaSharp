namespace NovaSharp.Interpreter.Tests.Units
{
    using System;
    using System.Linq.Expressions;
    using System.Reflection;
    using NovaSharp.Interpreter;
    using NovaSharp.Interpreter.DataTypes;
    using NovaSharp.Interpreter.Errors;
    using NovaSharp.Interpreter.Interop;
    using NovaSharp.Interpreter.Interop.Attributes;
    using NovaSharp.Interpreter.Interop.BasicDescriptors;
    using NovaSharp.Interpreter.Interop.StandardDescriptors.ReflectionMemberDescriptors;
    using NovaSharp.Interpreter.Platforms;
    using NUnit.Framework;

    [TestFixture]
    public sealed class PropertyMemberDescriptorTests
    {
        private readonly Script _script = new();

        [Test]
        public void TryCreateIfVisibleReturnsDescriptorForPublicProperty()
        {
            PropertyInfo property = SamplePropertiesMetadata.StaticValue;

            PropertyMemberDescriptor descriptor = PropertyMemberDescriptor.TryCreateIfVisible(
                property,
                InteropAccessMode.Reflection
            );

            Assert.That(descriptor, Is.Not.Null);
            Assert.Multiple(() =>
            {
                Assert.That(descriptor.Name, Is.EqualTo(nameof(SampleProperties.StaticValue)));
                Assert.That(descriptor.IsStatic, Is.True);
                Assert.That(descriptor.CanRead, Is.True);
                Assert.That(descriptor.CanWrite, Is.True);
            });
        }

        [Test]
        public void TryCreateIfVisibleRespectsHiddenAttribute()
        {
            PropertyInfo property = SamplePropertiesMetadata.HiddenProperty;

            PropertyMemberDescriptor descriptor = PropertyMemberDescriptor.TryCreateIfVisible(
                property,
                InteropAccessMode.Reflection
            );

            Assert.That(descriptor, Is.Null);
        }

        [Test]
        public void TryCreateIfVisibleHonorsAccessorVisibilityOverrides()
        {
            PropertyInfo property = SamplePropertiesMetadata.HiddenSetter;

            PropertyMemberDescriptor descriptor = PropertyMemberDescriptor.TryCreateIfVisible(
                property,
                InteropAccessMode.Reflection
            );

            Assert.That(descriptor, Is.Not.Null);
            Assert.Multiple(() =>
            {
                Assert.That(descriptor.CanRead, Is.True);
                Assert.That(descriptor.CanWrite, Is.False);
            });
        }

        [Test]
        public void TryCreateIfVisibleAllowsNonPublicPropertyMarkedVisible()
        {
            PropertyInfo property = SamplePropertiesMetadata.PrivateButVisible;

            PropertyMemberDescriptor descriptor = PropertyMemberDescriptor.TryCreateIfVisible(
                property,
                InteropAccessMode.Reflection
            );

            Assert.That(descriptor, Is.Not.Null);
            Assert.Multiple(() =>
            {
                Assert.That(descriptor.CanRead, Is.True);
                Assert.That(descriptor.CanWrite, Is.True);
            });
        }

        [Test]
        public void MemberAccessReflectsAvailableAccessors()
        {
            PropertyMemberDescriptor readWrite = new(
                SamplePropertiesMetadata.InstanceValue,
                InteropAccessMode.Reflection
            );
            PropertyMemberDescriptor readOnly = PropertyMemberDescriptor.TryCreateIfVisible(
                SamplePropertiesMetadata.GetterOnly,
                InteropAccessMode.Reflection
            );
            PropertyMemberDescriptor writeOnly = PropertyMemberDescriptor.TryCreateIfVisible(
                SamplePropertiesMetadata.SetterOnly,
                InteropAccessMode.Reflection
            );

            Assert.Multiple(() =>
            {
                Assert.That(
                    readWrite.MemberAccess,
                    Is.EqualTo(MemberDescriptorAccess.CanRead | MemberDescriptorAccess.CanWrite)
                );
                Assert.That(readOnly.MemberAccess, Is.EqualTo(MemberDescriptorAccess.CanRead));
                Assert.That(writeOnly.MemberAccess, Is.EqualTo(MemberDescriptorAccess.CanWrite));
            });
        }

        [Test]
        public void ConstructorThrowsWhenBothAccessorsMissing()
        {
            PropertyInfo property = SamplePropertiesMetadata.InstanceValue;

            ArgumentNullException exception = Assert.Throws<ArgumentNullException>(() =>
                new PropertyMemberDescriptor(property, InteropAccessMode.Reflection, null, null)
            );

            Assert.That(exception.ParamName, Is.Not.Null);
        }

        [Test]
        public void ConstructorThrowsWhenPropertyNull()
        {
            Assert.That(
                () => new PropertyMemberDescriptor(null, InteropAccessMode.Reflection, null, null),
                Throws.ArgumentNullException.With.Property("ParamName").EqualTo("pi")
            );
        }

        [Test]
        public void ConstructorFallsBackToReflectionWhenPlatformIsAot()
        {
            IPlatformAccessor original = Script.GlobalOptions.Platform;
            try
            {
                Script.GlobalOptions.Platform = new AotStubPlatformAccessor();
                PropertyMemberDescriptor descriptor = new(
                    SamplePropertiesMetadata.InstanceValue,
                    InteropAccessMode.Preoptimized
                );

                Assert.That(descriptor.AccessMode, Is.EqualTo(InteropAccessMode.Reflection));
            }
            finally
            {
                Script.GlobalOptions.Platform = original;
            }
        }

        [Test]
        public void GetValueReturnsInstanceValueWithLazyOptimization()
        {
            PropertyMemberDescriptor descriptor = new(
                SamplePropertiesMetadata.InstanceValue,
                InteropAccessMode.LazyOptimized
            );
            SampleProperties instance = new() { InstanceValue = 42 };

            DynValue value = descriptor.GetValue(_script, instance);

            Assert.That(value.Type, Is.EqualTo(DataType.Number));
            Assert.That(value.Number, Is.EqualTo(42));
        }

        [Test]
        public void GetValueThrowsWhenGetterMissing()
        {
            PropertyMemberDescriptor descriptor = PropertyMemberDescriptor.TryCreateIfVisible(
                SamplePropertiesMetadata.SetterOnly,
                InteropAccessMode.Reflection
            );

            ScriptRuntimeException exception = Assert.Throws<ScriptRuntimeException>(() =>
                descriptor.GetValue(_script, new SampleProperties())
            );

            Assert.That(exception.Message, Does.Contain("cannot be read"));
        }

        [Test]
        public void GetValueThrowsWhenInstanceMissing()
        {
            PropertyMemberDescriptor descriptor = new(
                SamplePropertiesMetadata.InstanceValue,
                InteropAccessMode.Reflection
            );

            ScriptRuntimeException exception = Assert.Throws<ScriptRuntimeException>(() =>
                descriptor.GetValue(_script, null)
            );

            Assert.That(exception.Message, Does.Contain("attempt to access instance member"));
        }

        [Test]
        public void SetValueUpdatesInstancePropertyAndConvertsNumbers()
        {
            PropertyMemberDescriptor descriptor = new(
                SamplePropertiesMetadata.InstanceValue,
                InteropAccessMode.Reflection
            );
            SampleProperties instance = new();

            descriptor.SetValue(_script, instance, DynValue.NewNumber(5.1));

            Assert.That(instance.InstanceValue, Is.EqualTo(5));
        }

        [Test]
        public void SetValueThrowsWhenSetterMissing()
        {
            PropertyMemberDescriptor descriptor = PropertyMemberDescriptor.TryCreateIfVisible(
                SamplePropertiesMetadata.GetterOnly,
                InteropAccessMode.Reflection
            );

            ScriptRuntimeException exception = Assert.Throws<ScriptRuntimeException>(() =>
                descriptor.SetValue(_script, new SampleProperties(), DynValue.NewNumber(1))
            );

            Assert.That(exception.Message, Does.Contain("cannot be assigned"));
        }

        [Test]
        public void SetValueThrowsWhenDynValueNull()
        {
            PropertyMemberDescriptor descriptor = new(
                SamplePropertiesMetadata.InstanceValue,
                InteropAccessMode.Reflection
            );

            Assert.That(
                () => descriptor.SetValue(_script, new SampleProperties(), null),
                Throws.ArgumentNullException.With.Property("ParamName").EqualTo("value")
            );
        }

        [Test]
        public void GetValueThrowsWhenScriptMissing()
        {
            PropertyMemberDescriptor descriptor = new(
                SamplePropertiesMetadata.InstanceValue,
                InteropAccessMode.Reflection
            );

            Assert.That(() => descriptor.GetValue(null, new SampleProperties()), Throws.Nothing);
        }

        [Test]
        public void SetValueThrowsWhenScriptMissing()
        {
            PropertyMemberDescriptor descriptor = new(
                SamplePropertiesMetadata.InstanceValue,
                InteropAccessMode.Reflection
            );

            Assert.That(
                () => descriptor.SetValue(null, new SampleProperties(), DynValue.NewNumber(1)),
                Throws.Nothing
            );
        }

        [Test]
        public void SetValueThrowsOnTypeMismatch()
        {
            PropertyMemberDescriptor descriptor = new(
                SamplePropertiesMetadata.InstanceValue,
                InteropAccessMode.Reflection
            );

            ScriptRuntimeException exception = Assert.Throws<ScriptRuntimeException>(() =>
                descriptor.SetValue(_script, new SampleProperties(), DynValue.NewString("bad"))
            );

            Assert.That(exception.Message, Does.Contain("cannot convert a string"));
        }

        [Test]
        public void SetValueConvertsDoubleToPropertyType()
        {
            PropertyMemberDescriptor descriptor = PropertyMemberDescriptor.TryCreateIfVisible(
                SamplePropertiesMetadata.ShortValue,
                InteropAccessMode.Reflection
            );

            SampleProperties instance = new();
            descriptor.SetValue(_script, instance, DynValue.NewNumber(12.0));

            Assert.That(instance.ShortValue, Is.EqualTo((short)12));
        }

        [Test]
        public void SetValueWrapsArgumentExceptionFromOptimizedSetter()
        {
            PropertyMemberDescriptor descriptor = new(
                SamplePropertiesMetadata.InstanceValue,
                InteropAccessMode.Reflection
            );
            OverrideOptimizedSetter(
                descriptor,
                (_, _) => throw new ArgumentException("failing reflection assignment")
            );

            ScriptRuntimeException exception = Assert.Throws<ScriptRuntimeException>(() =>
                descriptor.SetValue(_script, new SampleProperties(), DynValue.NewNumber(1))
            )!;

            Assert.That(exception.Message, Does.Contain("cannot find a conversion"));
        }

        [Test]
        public void SetValueWrapsInvalidCastExceptionFromOptimizedSetter()
        {
            PropertyMemberDescriptor descriptor = new(
                SamplePropertiesMetadata.InstanceValue,
                InteropAccessMode.Reflection
            );
            OverrideOptimizedSetter(
                descriptor,
                (_, _) => throw new InvalidCastException("failing optimized assignment")
            );

            ScriptRuntimeException exception = Assert.Throws<ScriptRuntimeException>(() =>
                descriptor.SetValue(_script, new SampleProperties(), DynValue.NewNumber(1))
            )!;

            Assert.That(exception.Message, Does.Contain("cannot find a conversion"));
        }

        [Test]
        public void SetValueThrowsWhenInstanceMissing()
        {
            PropertyMemberDescriptor descriptor = new(
                SamplePropertiesMetadata.InstanceValue,
                InteropAccessMode.Reflection
            );

            ScriptRuntimeException exception = Assert.Throws<ScriptRuntimeException>(() =>
                descriptor.SetValue(_script, null, DynValue.NewNumber(1))
            );

            Assert.That(exception.Message, Does.Contain("attempt to access instance member"));
        }

        [Test]
        public void SetValueOptimizesStaticSetterWhenLazy()
        {
            SampleProperties.LazyStatic = 0;
            PropertyMemberDescriptor descriptor = new(
                SamplePropertiesMetadata.LazyStatic,
                InteropAccessMode.LazyOptimized
            );

            descriptor.SetValue(_script, null, DynValue.NewNumber(9));

            Assert.That(SampleProperties.LazyStatic, Is.EqualTo(9));
            SampleProperties.LazyStatic = 0;
        }

        [Test]
        public void OptimizePrecompilesGetterAndSetter()
        {
            PropertyMemberDescriptor descriptor = new(
                SamplePropertiesMetadata.InstanceValue,
                InteropAccessMode.LazyOptimized
            );
            SampleProperties instance = new() { InstanceValue = 12 };

            ((IOptimizableDescriptor)descriptor).Optimize();

            Assert.That(descriptor.GetValue(_script, instance).Number, Is.EqualTo(12));
            descriptor.SetValue(_script, instance, DynValue.NewNumber(4));
            Assert.That(instance.InstanceValue, Is.EqualTo(4));
        }

        [Test]
        public void PrepareForWiringPopulatesMetadataTable()
        {
            PropertyMemberDescriptor descriptor = new(
                SamplePropertiesMetadata.InstanceValue,
                InteropAccessMode.Reflection
            );
            Table metadata = new(_script);

            descriptor.PrepareForWiring(metadata);

            Assert.Multiple(() =>
            {
                Assert.That(
                    metadata.Get("name").String,
                    Is.EqualTo(nameof(SampleProperties.InstanceValue))
                );
                Assert.That(metadata.Get("static").Boolean, Is.False);
                Assert.That(metadata.Get("read").Boolean, Is.True);
                Assert.That(metadata.Get("write").Boolean, Is.True);
                Assert.That(metadata.Get("declvtype").Boolean, Is.False);
            });
        }

        [Test]
        public void PrepareForWiringThrowsWhenTableNull()
        {
            PropertyMemberDescriptor descriptor = new(
                SamplePropertiesMetadata.InstanceValue,
                InteropAccessMode.Reflection
            );

            Assert.That(
                () => descriptor.PrepareForWiring(null),
                Throws.ArgumentNullException.With.Property("ParamName").EqualTo("t")
            );
        }

        private static void OverrideOptimizedSetter(
            PropertyMemberDescriptor descriptor,
            Action<object, object> setter
        )
        {
            PropertyMemberDescriptor.TestHooks.SetOptimizedSetter(descriptor, setter);
        }

        private sealed class SampleProperties
        {
            public static int StaticValue { get; set; } = 10;

            public static int LazyStatic { get; set; }

            public int InstanceValue { get; set; }

            public int SetterOnly
            {
                set { SetterOnlyBacking = value; }
            }

            public int SetterOnlyBacking { get; private set; }

            public int GetterOnly { get; } = 7;

            public short ShortValue { get; set; }

            [NovaSharpHidden]
            public int HiddenProperty { get; set; }

            public int HiddenSetter
            {
                get { return _hiddenSetterBacking; }
                [NovaSharpHidden]
                set { _hiddenSetterBacking = value; }
            }

            [NovaSharpVisible(true)]
            private int PrivateButVisible { get; set; } = 6;

            private int _hiddenSetterBacking = 5;

            internal static PropertyInfo PrivateButVisibleProperty { get; } =
                GetPropertyInfo(p => p.PrivateButVisible);

            private static PropertyInfo GetPropertyInfo<TValue>(
                Expression<Func<SampleProperties, TValue>> accessor
            )
            {
                return (PropertyInfo)GetMember(accessor.Body);
            }

            private static MemberInfo GetMember(Expression expression)
            {
                if (expression is MemberExpression member)
                {
                    return member.Member;
                }

                if (
                    expression is UnaryExpression unary
                    && unary.NodeType == ExpressionType.Convert
                    && unary.Operand is MemberExpression unaryMember
                )
                {
                    return unaryMember.Member;
                }

                throw new InvalidOperationException(
                    "Expected member expression for property access."
                );
            }
        }

        internal static class SamplePropertiesMetadata
        {
            internal static PropertyInfo StaticValue { get; } =
                typeof(SampleProperties).GetProperty(nameof(SampleProperties.StaticValue))!;

            internal static PropertyInfo LazyStatic { get; } =
                typeof(SampleProperties).GetProperty(nameof(SampleProperties.LazyStatic))!;

            internal static PropertyInfo InstanceValue { get; } =
                typeof(SampleProperties).GetProperty(nameof(SampleProperties.InstanceValue))!;

            internal static PropertyInfo SetterOnly { get; } =
                typeof(SampleProperties).GetProperty(nameof(SampleProperties.SetterOnly))!;

            internal static PropertyInfo GetterOnly { get; } =
                typeof(SampleProperties).GetProperty(nameof(SampleProperties.GetterOnly))!;

            internal static PropertyInfo ShortValue { get; } =
                typeof(SampleProperties).GetProperty(nameof(SampleProperties.ShortValue))!;

            internal static PropertyInfo HiddenProperty { get; } =
                typeof(SampleProperties).GetProperty(nameof(SampleProperties.HiddenProperty))!;

            internal static PropertyInfo HiddenSetter { get; } =
                typeof(SampleProperties).GetProperty(nameof(SampleProperties.HiddenSetter))!;

            internal static PropertyInfo PrivateButVisible { get; } =
                SampleProperties.PrivateButVisibleProperty;
        }
    }
}
