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
    [Parallelizable(ParallelScope.Self)]
    [ScriptGlobalOptionsIsolation]
    public sealed class FieldMemberDescriptorTests
    {
        private readonly Script _script = new();

        [Test]
        public void TryCreateIfVisibleReturnsDescriptorForPublicField()
        {
            FieldInfo fieldInfo = SampleFieldsMetadata.StaticValue;

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
            FieldInfo fieldInfo = SampleFieldsMetadata.PrivateValue;

            FieldMemberDescriptor descriptor = FieldMemberDescriptor.TryCreateIfVisible(
                fieldInfo,
                InteropAccessMode.Reflection
            );

            Assert.That(descriptor, Is.Null);
        }

        [Test]
        public void TryCreateIfVisibleHonorsVisibilityAttribute()
        {
            FieldInfo fieldInfo = SampleFieldsMetadata.AttributeValue;

            FieldMemberDescriptor descriptor = FieldMemberDescriptor.TryCreateIfVisible(
                fieldInfo,
                InteropAccessMode.Reflection
            );

            Assert.That(descriptor, Is.Not.Null);
            Assert.That(descriptor.Name, Is.EqualTo("_attributeValue"));
        }

        [Test]
        public void TryCreateIfVisibleThrowsWhenFieldNull()
        {
            Assert.That(
                () => FieldMemberDescriptor.TryCreateIfVisible(null, InteropAccessMode.Reflection),
                Throws.ArgumentNullException.With.Property("ParamName").EqualTo("fi")
            );
        }

        [Test]
        public void ConstructorThrowsWhenFieldNull()
        {
            Assert.That(
                () => new FieldMemberDescriptor(null, InteropAccessMode.Reflection),
                Throws.ArgumentNullException.With.Property("ParamName").EqualTo("fi")
            );
        }

        [Test]
        public void MemberAccessReflectsConstAndReadonlyState()
        {
            FieldMemberDescriptor constDescriptor = new(
                SampleFieldsMetadata.ConstValue,
                InteropAccessMode.Reflection
            );
            FieldMemberDescriptor readonlyDescriptor = new(
                SampleFieldsMetadata.ReadonlyValue,
                InteropAccessMode.Reflection
            );
            FieldMemberDescriptor writableDescriptor = new(
                SampleFieldsMetadata.StaticValue,
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
                SampleFieldsMetadata.ConstValue,
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
                SampleFieldsMetadata.StaticValue,
                InteropAccessMode.Preoptimized
            );

            Func<object, object> optimizedGetter = descriptor.OptimizedGetter;

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
                SampleFieldsMetadata.StaticValue,
                InteropAccessMode.LazyOptimized
            );

            Assert.That(descriptor.OptimizedGetter, Is.Null, "Getter not yet compiled");

            DynValue first = descriptor.GetValue(_script, null);
            Func<object, object> optimizedGetter = descriptor.OptimizedGetter;
            DynValue second = descriptor.GetValue(_script, null);

            Assert.Multiple(() =>
            {
                Assert.That(first.Number, Is.EqualTo(SampleFields.StaticValue));
                Assert.That(optimizedGetter, Is.Not.Null, "Getter compiled after first access");
                Assert.That(second.Number, Is.EqualTo(SampleFields.StaticValue));
            });
        }

        [Test]
        public void GetValueReturnsInstanceFieldViaReflection()
        {
            SampleFields instance = new() { _instanceValue = 42 };
            FieldMemberDescriptor descriptor = new(
                SampleFieldsMetadata.InstanceValue,
                InteropAccessMode.Reflection
            );

            DynValue result = descriptor.GetValue(_script, instance);

            Assert.Multiple(() =>
            {
                Assert.That(descriptor.OptimizedGetter, Is.Null);
                Assert.That(result.Type, Is.EqualTo(DataType.Number));
                Assert.That(result.Number, Is.EqualTo(instance._instanceValue));
            });
        }

        [Test]
        public void PreoptimizedGetterCompilesForInstanceField()
        {
            SampleFields instance = new() { _instanceValue = 99 };
            FieldMemberDescriptor descriptor = new(
                SampleFieldsMetadata.InstanceValue,
                InteropAccessMode.Preoptimized
            );

            DynValue result = descriptor.GetValue(_script, instance);

            Assert.Multiple(() =>
            {
                Assert.That(descriptor.OptimizedGetter, Is.Not.Null);
                Assert.That(result.Number, Is.EqualTo(instance._instanceValue));
            });
        }

        [Test]
        public void PreoptimizedConstFieldDoesNotCompileGetter()
        {
            FieldMemberDescriptor descriptor = new(
                SampleFieldsMetadata.ConstValue,
                InteropAccessMode.Preoptimized
            );

            DynValue result = descriptor.GetValue(_script, null);

            Assert.Multiple(() =>
            {
                Assert.That(descriptor.OptimizedGetter, Is.Null);
                Assert.That(result.Number, Is.EqualTo(SampleFields.ConstValue));
            });
        }

        [Test]
        public void ConstructorForcesReflectionModeOnAotPlatforms()
        {
            IPlatformAccessor original = Script.GlobalOptions.Platform;

            try
            {
                Script.GlobalOptions.Platform = new AotStubPlatformAccessor();
                FieldMemberDescriptor descriptor = new(
                    SampleFieldsMetadata.StaticValue,
                    InteropAccessMode.Preoptimized
                );

                Assert.Multiple(() =>
                {
                    Assert.That(descriptor.AccessMode, Is.EqualTo(InteropAccessMode.Reflection));
                    Assert.That(descriptor.OptimizedGetter, Is.Null);
                });
            }
            finally
            {
                Script.GlobalOptions.Platform = original;
            }
        }

        [Test]
        public void OptimizeInterfaceCompilesGetterOnDemand()
        {
            FieldMemberDescriptor descriptor = new(
                SampleFieldsMetadata.InstanceValue,
                InteropAccessMode.Reflection
            );
            SampleFields instance = new() { _instanceValue = 64 };

            Assert.That(descriptor.OptimizedGetter, Is.Null);

            ((IOptimizableDescriptor)descriptor).Optimize();

            DynValue value = descriptor.GetValue(_script, instance);

            Assert.Multiple(() =>
            {
                Assert.That(descriptor.OptimizedGetter, Is.Not.Null);
                Assert.That(value.Number, Is.EqualTo(instance._instanceValue));
            });
        }

        [Test]
        public void SetValueRejectsConstAndReadonlyFields()
        {
            FieldMemberDescriptor constDescriptor = new(
                SampleFieldsMetadata.ConstValue,
                InteropAccessMode.Reflection
            );
            FieldMemberDescriptor readonlyDescriptor = new(
                SampleFieldsMetadata.ReadonlyValue,
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
                SampleFieldsMetadata.InstanceValue,
                InteropAccessMode.Reflection
            );

            descriptor.SetValue(_script, instance, DynValue.NewNumber(5.0));

            Assert.That(instance._instanceValue, Is.EqualTo(5));
        }

        [Test]
        public void SetValueThrowsWhenTypeMismatchOccurs()
        {
            SampleFields instance = new();
            FieldMemberDescriptor descriptor = new(
                SampleFieldsMetadata.InstanceValue,
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
                SampleFieldsMetadata.InstanceValue,
                InteropAccessMode.Reflection
            );

            Assert.That(
                () => descriptor.SetValue(_script, null, DynValue.NewNumber(1)),
                Throws
                    .TypeOf<ScriptRuntimeException>()
                    .With.Message.Contains("attempt to access instance member _instanceValue")
            );
        }

        [Test]
        public void SetValueThrowsWhenInstanceTypeDoesNotMatchField()
        {
            FieldMemberDescriptor descriptor = new(
                SampleFieldsMetadata.InstanceValue,
                InteropAccessMode.Reflection
            );

            ScriptRuntimeException exception = Assert.Throws<ScriptRuntimeException>(() =>
                descriptor.SetValue(_script, new object(), DynValue.NewNumber(1))
            );

            Assert.That(exception.Message, Does.Contain("cannot find a conversion"));
        }

        [Test]
        public void SetValueNormalizesDoubleValuesThroughNumericConversions()
        {
            SampleFields instance = new();
            FieldMemberDescriptor descriptor = new(
                SampleFieldsMetadata.DoubleValue,
                InteropAccessMode.Reflection
            );

            descriptor.SetValue(_script, instance, DynValue.NewNumber(2.5));

            Assert.That(instance._doubleValue, Is.EqualTo(2.5));
        }

        [Test]
        public void PrepareForWiringPopulatesMetadataTable()
        {
            FieldMemberDescriptor descriptor = new(
                SampleFieldsMetadata.StaticValue,
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

        [Test]
        public void PrepareForWiringThrowsWhenTableNull()
        {
            FieldMemberDescriptor descriptor = new(
                SampleFieldsMetadata.StaticValue,
                InteropAccessMode.Reflection
            );

            Assert.That(
                () => descriptor.PrepareForWiring(null),
                Throws.ArgumentNullException.With.Property("ParamName").EqualTo("t")
            );
        }

        private sealed class SampleFields
        {
            public const int ConstValue = 7;
            public static readonly string ReadonlyValue = new string(
                new[] { 'f', 'i', 'x', 'e', 'd' }
            );
            public static int StaticValue = 1;
            internal int _instanceValue;
            internal double _doubleValue;

            [NovaSharpVisible(true)]
            private int _attributeValue;
            private int _privateValue;

            public int AttributeValue
            {
                get => _attributeValue;
                set => _attributeValue = value;
            }

            public SampleFields()
            {
                _instanceValue = 0;
                _doubleValue = 0;
                _privateValue = 0;
                AnchorAttributeValueUsage();
            }

            internal static FieldInfo PrivateValueField { get; } =
                GetFieldInfo(f => f._privateValue);

            private static FieldInfo GetFieldInfo<TValue>(
                Expression<Func<SampleFields, TValue>> accessor
            )
            {
                return (FieldInfo)GetMember(accessor.Body);
            }

            private void AnchorAttributeValueUsage()
            {
                GC.KeepAlive(_attributeValue);
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

                throw new InvalidOperationException("Expected member expression for field access.");
            }
        }

        private static class SampleFieldsMetadata
        {
            internal static FieldInfo ConstValue { get; } =
                typeof(SampleFields).GetField(nameof(SampleFields.ConstValue))!;

            internal static FieldInfo ReadonlyValue { get; } =
                typeof(SampleFields).GetField(nameof(SampleFields.ReadonlyValue))!;

            internal static FieldInfo StaticValue { get; } =
                typeof(SampleFields).GetField(nameof(SampleFields.StaticValue))!;

            internal static FieldInfo InstanceValue { get; } =
                typeof(SampleFields).GetField(
                    nameof(SampleFields._instanceValue),
                    BindingFlags.Instance | BindingFlags.NonPublic
                )!;

            internal static FieldInfo DoubleValue { get; } =
                typeof(SampleFields).GetField(
                    nameof(SampleFields._doubleValue),
                    BindingFlags.Instance | BindingFlags.NonPublic
                )!;

            internal static FieldInfo PrivateValue { get; } = SampleFields.PrivateValueField;

            internal static FieldInfo AttributeValue { get; } =
                typeof(SampleFields).GetField(
                    "_attributeValue",
                    BindingFlags.Instance | BindingFlags.NonPublic
                )!;
        }
    }
}
