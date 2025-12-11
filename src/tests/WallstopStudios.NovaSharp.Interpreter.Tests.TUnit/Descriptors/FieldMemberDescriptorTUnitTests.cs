namespace WallstopStudios.NovaSharp.Interpreter.Tests.TUnit.Descriptors
{
    using System;
    using System.Linq.Expressions;
    using System.Reflection;
    using System.Threading.Tasks;
    using global::TUnit.Assertions;
    using WallstopStudios.NovaSharp.Interpreter;
    using WallstopStudios.NovaSharp.Interpreter.DataTypes;
    using WallstopStudios.NovaSharp.Interpreter.Errors;
    using WallstopStudios.NovaSharp.Interpreter.Interop;
    using WallstopStudios.NovaSharp.Interpreter.Interop.Attributes;
    using WallstopStudios.NovaSharp.Interpreter.Interop.BasicDescriptors;
    using WallstopStudios.NovaSharp.Interpreter.Interop.StandardDescriptors.ReflectionMemberDescriptors;
    using WallstopStudios.NovaSharp.Interpreter.Platforms;
    using WallstopStudios.NovaSharp.Interpreter.Tests;
    using WallstopStudios.NovaSharp.Tests.TestInfrastructure.Scopes;

    [ScriptGlobalOptionsIsolation]
    [PlatformDetectorIsolation]
    public sealed class FieldMemberDescriptorTUnitTests
    {
        [global::TUnit.Core.Test]
        public async Task TryCreateIfVisibleReturnsDescriptorForPublicField()
        {
            FieldInfo fieldInfo = SampleFieldsMetadata.StaticValue;

            FieldMemberDescriptor descriptor = FieldMemberDescriptor.TryCreateIfVisible(
                fieldInfo,
                InteropAccessMode.Reflection
            );

            await Assert.That(descriptor).IsNotNull();
            await Assert.That(descriptor.Name).IsEqualTo(nameof(SampleFields.StaticValue));
            await Assert.That(descriptor.IsStatic).IsTrue();
        }

        [global::TUnit.Core.Test]
        public async Task TryCreateIfVisibleRejectsNonPublicField()
        {
            FieldInfo fieldInfo = SampleFieldsMetadata.PrivateValue;

            FieldMemberDescriptor descriptor = FieldMemberDescriptor.TryCreateIfVisible(
                fieldInfo,
                InteropAccessMode.Reflection
            );

            await Assert.That(descriptor).IsNull();
        }

        [global::TUnit.Core.Test]
        public async Task TryCreateIfVisibleHonorsVisibilityAttribute()
        {
            FieldInfo fieldInfo = SampleFieldsMetadata.AttributeValue;

            FieldMemberDescriptor descriptor = FieldMemberDescriptor.TryCreateIfVisible(
                fieldInfo,
                InteropAccessMode.Reflection
            );

            await Assert.That(descriptor).IsNotNull();
            await Assert.That(descriptor.Name).IsEqualTo("_attributeValue");
        }

        [global::TUnit.Core.Test]
        public async Task TryCreateIfVisibleThrowsWhenFieldNull()
        {
            ArgumentNullException exception = ExpectException<ArgumentNullException>(() =>
                FieldMemberDescriptor.TryCreateIfVisible(null, InteropAccessMode.Reflection)
            );

            await Assert.That(exception.ParamName).IsEqualTo("fi");
        }

        [global::TUnit.Core.Test]
        public async Task ConstructorThrowsWhenFieldNull()
        {
            ArgumentNullException exception = ExpectException<ArgumentNullException>(() =>
            {
                _ = new FieldMemberDescriptor(null, InteropAccessMode.Reflection);
            });

            await Assert.That(exception.ParamName).IsEqualTo("fi");
        }

        [global::TUnit.Core.Test]
        public async Task MemberAccessReflectsConstAndReadonlyState()
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

            await Assert
                .That(constDescriptor.MemberAccess)
                .IsEqualTo(MemberDescriptorAccess.CanRead);
            await Assert
                .That(readonlyDescriptor.MemberAccess)
                .IsEqualTo(MemberDescriptorAccess.CanRead);
            await Assert
                .That(writableDescriptor.MemberAccess)
                .IsEqualTo(MemberDescriptorAccess.CanRead | MemberDescriptorAccess.CanWrite);
        }

        [global::TUnit.Core.Test]
        public async Task GetValueReturnsConstFieldWithoutAllocatingInstance()
        {
            FieldMemberDescriptor descriptor = new(
                SampleFieldsMetadata.ConstValue,
                InteropAccessMode.Reflection
            );

            DynValue value = descriptor.GetValue(CreateScript(), null);

            await Assert.That(value.Type).IsEqualTo(DataType.Number);
            await Assert.That(value.Number).IsEqualTo(SampleFields.ConstValue);
        }

        [global::TUnit.Core.Test]
        public async Task GetValueUsingPreoptimizedGetterReturnsStaticField()
        {
            using PlatformDetectorOverrideScope platformScope =
                PlatformDetectorOverrideScope.SetRunningOnAot(false);
            FieldMemberDescriptor descriptor = new(
                SampleFieldsMetadata.StaticValue,
                InteropAccessMode.Preoptimized
            );

            await Assert.That(descriptor.AccessMode).IsEqualTo(InteropAccessMode.Preoptimized);
            Func<object, object> optimizedGetter = descriptor.OptimizedGetter;
            double number = descriptor.GetValue(CreateScript(), null).Number;

            await Assert.That(optimizedGetter).IsNotNull();
            await Assert.That(number).IsEqualTo(SampleFields.StaticValue);
        }

        [global::TUnit.Core.Test]
        public async Task LazyOptimizedGetterCompilesOnFirstAccess()
        {
            using PlatformDetectorOverrideScope platformScope =
                PlatformDetectorOverrideScope.SetRunningOnAot(false);
            FieldMemberDescriptor descriptor = new(
                SampleFieldsMetadata.StaticValue,
                InteropAccessMode.LazyOptimized
            );

            await Assert.That(descriptor.AccessMode).IsEqualTo(InteropAccessMode.LazyOptimized);
            await Assert.That(descriptor.OptimizedGetter).IsNull();

            DynValue first = descriptor.GetValue(CreateScript(), null);
            Func<object, object> optimizedGetter = descriptor.OptimizedGetter;
            DynValue second = descriptor.GetValue(CreateScript(), null);

            await Assert.That(first.Number).IsEqualTo(SampleFields.StaticValue);
            await Assert.That(optimizedGetter).IsNotNull();
            await Assert.That(second.Number).IsEqualTo(SampleFields.StaticValue);
        }

        [global::TUnit.Core.Test]
        public async Task GetValueReturnsInstanceFieldViaReflection()
        {
            SampleFields instance = new() { _instanceValue = 42 };
            FieldMemberDescriptor descriptor = new(
                SampleFieldsMetadata.InstanceValue,
                InteropAccessMode.Reflection
            );

            DynValue result = descriptor.GetValue(CreateScript(), instance);

            await Assert.That(descriptor.OptimizedGetter).IsNull();
            await Assert.That(result.Type).IsEqualTo(DataType.Number);
            await Assert.That(result.Number).IsEqualTo(instance._instanceValue);
        }

        [global::TUnit.Core.Test]
        public async Task PreoptimizedGetterCompilesForInstanceField()
        {
            using PlatformDetectorOverrideScope platformScope =
                PlatformDetectorOverrideScope.SetRunningOnAot(false);
            SampleFields instance = new() { _instanceValue = 99 };
            FieldMemberDescriptor descriptor = new(
                SampleFieldsMetadata.InstanceValue,
                InteropAccessMode.Preoptimized
            );

            await Assert.That(descriptor.AccessMode).IsEqualTo(InteropAccessMode.Preoptimized);
            DynValue result = descriptor.GetValue(CreateScript(), instance);

            await Assert.That(descriptor.OptimizedGetter).IsNotNull();
            await Assert.That(result.Number).IsEqualTo(instance._instanceValue);
        }

        [global::TUnit.Core.Test]
        public async Task PreoptimizedConstFieldDoesNotCompileGetter()
        {
            FieldMemberDescriptor descriptor = new(
                SampleFieldsMetadata.ConstValue,
                InteropAccessMode.Preoptimized
            );

            DynValue result = descriptor.GetValue(CreateScript(), null);

            await Assert.That(descriptor.OptimizedGetter).IsNull();
            await Assert.That(result.Number).IsEqualTo(SampleFields.ConstValue);
        }

        [global::TUnit.Core.Test]
        public async Task ConstructorForcesReflectionModeOnAotPlatforms()
        {
            using PlatformDetectorOverrideScope platformScope =
                PlatformDetectorOverrideScope.SetRunningOnAot(true);
            FieldMemberDescriptor descriptor = new(
                SampleFieldsMetadata.StaticValue,
                InteropAccessMode.Preoptimized
            );

            await Assert.That(descriptor.AccessMode).IsEqualTo(InteropAccessMode.Reflection);
            await Assert.That(descriptor.OptimizedGetter).IsNull();
        }

        [global::TUnit.Core.Test]
        public async Task OptimizeInterfaceCompilesGetterOnDemand()
        {
            FieldMemberDescriptor descriptor = new(
                SampleFieldsMetadata.InstanceValue,
                InteropAccessMode.Reflection
            );
            SampleFields instance = new() { _instanceValue = 64 };

            await Assert.That(descriptor.OptimizedGetter).IsNull();

            ((IOptimizableDescriptor)descriptor).Optimize();

            DynValue value = descriptor.GetValue(CreateScript(), instance);

            await Assert.That(descriptor.OptimizedGetter).IsNotNull();
            await Assert.That(value.Number).IsEqualTo(instance._instanceValue);
        }

        [global::TUnit.Core.Test]
        public async Task SetValueRejectsConstAndReadonlyFields()
        {
            FieldMemberDescriptor constDescriptor = new(
                SampleFieldsMetadata.ConstValue,
                InteropAccessMode.Reflection
            );
            FieldMemberDescriptor readonlyDescriptor = new(
                SampleFieldsMetadata.ReadonlyValue,
                InteropAccessMode.Reflection
            );

            ScriptRuntimeException constException = ExpectException<ScriptRuntimeException>(() =>
                constDescriptor.SetValue(CreateScript(), null, DynValue.NewNumber(10))
            );
            ScriptRuntimeException readonlyException = ExpectException<ScriptRuntimeException>(() =>
                readonlyDescriptor.SetValue(CreateScript(), null, DynValue.NewString("new"))
            );

            await Assert.That(constException.Message).Contains("cannot be assigned");
            await Assert.That(readonlyException.Message).Contains("cannot be assigned");
        }

        [global::TUnit.Core.Test]
        public async Task SetValueConvertsNumericDynValue()
        {
            SampleFields instance = new();
            FieldMemberDescriptor descriptor = new(
                SampleFieldsMetadata.InstanceValue,
                InteropAccessMode.Reflection
            );

            descriptor.SetValue(CreateScript(), instance, DynValue.NewNumber(5));

            await Assert.That(instance._instanceValue).IsEqualTo(5);
        }

        [global::TUnit.Core.Test]
        public async Task SetValueThrowsWhenTypeMismatchOccurs()
        {
            SampleFields instance = new();
            FieldMemberDescriptor descriptor = new(
                SampleFieldsMetadata.InstanceValue,
                InteropAccessMode.Reflection
            );

            ScriptRuntimeException exception = ExpectException<ScriptRuntimeException>(() =>
                descriptor.SetValue(CreateScript(), instance, DynValue.NewString("invalid"))
            );

            await Assert.That(exception.Message).Contains("cannot convert");
        }

        [global::TUnit.Core.Test]
        public async Task SetValueThrowsWhenInstanceIsMissing()
        {
            FieldMemberDescriptor descriptor = new(
                SampleFieldsMetadata.InstanceValue,
                InteropAccessMode.Reflection
            );

            ScriptRuntimeException exception = ExpectException<ScriptRuntimeException>(() =>
                descriptor.SetValue(CreateScript(), null, DynValue.NewNumber(1))
            );

            await Assert.That(exception.Message).Contains("attempt to access instance member");
        }

        [global::TUnit.Core.Test]
        public async Task SetValueThrowsWhenInstanceTypeDoesNotMatchField()
        {
            FieldMemberDescriptor descriptor = new(
                SampleFieldsMetadata.InstanceValue,
                InteropAccessMode.Reflection
            );

            ScriptRuntimeException exception = ExpectException<ScriptRuntimeException>(() =>
                descriptor.SetValue(CreateScript(), new object(), DynValue.NewNumber(1))
            );

            await Assert.That(exception.Message).Contains("cannot find a conversion");
        }

        [global::TUnit.Core.Test]
        public async Task SetValueNormalizesDoubleValuesThroughNumericConversions()
        {
            SampleFields instance = new();
            FieldMemberDescriptor descriptor = new(
                SampleFieldsMetadata.DoubleValue,
                InteropAccessMode.Reflection
            );

            descriptor.SetValue(CreateScript(), instance, DynValue.NewNumber(2.5));

            await Assert.That(instance._doubleValue).IsEqualTo(2.5d);
        }

        [global::TUnit.Core.Test]
        public async Task PrepareForWiringPopulatesMetadataTable()
        {
            FieldMemberDescriptor descriptor = new(
                SampleFieldsMetadata.StaticValue,
                InteropAccessMode.Reflection
            );
            Script script = CreateScript();
            Table table = new(script);

            descriptor.PrepareForWiring(table);

            await Assert.That(table.Get("name").String).IsEqualTo(nameof(SampleFields.StaticValue));
            await Assert.That(table.Get("static").Boolean).IsTrue();
            await Assert.That(table.Get("write").Boolean).IsTrue();
            await Assert.That(table.Get("const").Boolean).IsFalse();
        }

        [global::TUnit.Core.Test]
        public async Task PrepareForWiringThrowsWhenTableNull()
        {
            FieldMemberDescriptor descriptor = new(
                SampleFieldsMetadata.StaticValue,
                InteropAccessMode.Reflection
            );

            ArgumentNullException exception = ExpectException<ArgumentNullException>(() =>
                descriptor.PrepareForWiring(null)
            );

            await Assert.That(exception.ParamName).IsEqualTo("t");
        }

        private static Script CreateScript()
        {
            return new Script();
        }

        private static TException ExpectException<TException>(Action action)
            where TException : Exception
        {
            try
            {
                action();
            }
            catch (TException exception)
            {
                return exception;
            }

            throw new InvalidOperationException(
                $"Expected exception of type {typeof(TException).Name}."
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
                GetFieldInfo(value => value._privateValue);

            private static FieldInfo GetFieldInfo<TValue>(
                Expression<Func<SampleFields, TValue>> accessor
            )
            {
                return (FieldInfo)GetMember(accessor.Body);
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

            private void AnchorAttributeValueUsage()
            {
                GC.KeepAlive(_attributeValue);
            }
        }

        private static class SampleFieldsMetadata
        {
            internal static FieldInfo ConstValue { get; } =
                GetRequiredField(
                    nameof(SampleFields.ConstValue),
                    BindingFlags.Public | BindingFlags.Static
                );

            internal static FieldInfo ReadonlyValue { get; } =
                GetRequiredField(
                    nameof(SampleFields.ReadonlyValue),
                    BindingFlags.Public | BindingFlags.Static
                );

            internal static FieldInfo StaticValue { get; } =
                GetRequiredField(
                    nameof(SampleFields.StaticValue),
                    BindingFlags.Public | BindingFlags.Static
                );

            internal static FieldInfo InstanceValue { get; } =
                GetRequiredField("_instanceValue", BindingFlags.Instance | BindingFlags.NonPublic);

            internal static FieldInfo DoubleValue { get; } =
                GetRequiredField("_doubleValue", BindingFlags.Instance | BindingFlags.NonPublic);

            internal static FieldInfo PrivateValue { get; } = SampleFields.PrivateValueField;

            internal static FieldInfo AttributeValue { get; } =
                GetRequiredField("_attributeValue", BindingFlags.Instance | BindingFlags.NonPublic);

            private static FieldInfo GetRequiredField(string name, BindingFlags bindingFlags)
            {
                FieldInfo field = typeof(SampleFields).GetField(name, bindingFlags);
                if (field == null)
                {
                    throw new InvalidOperationException($"Field '{name}' not found.");
                }

                return field;
            }
        }
    }
}
