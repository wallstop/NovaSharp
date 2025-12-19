namespace WallstopStudios.NovaSharp.Interpreter.Tests.TUnit.Descriptors
{
    using System;
    using System.Linq.Expressions;
    using System.Reflection;
    using System.Threading.Tasks;
    using global::TUnit.Assertions;
    using WallstopStudios.NovaSharp.Interpreter;
    using WallstopStudios.NovaSharp.Interpreter.Compatibility;
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
    public sealed class PropertyMemberDescriptorTUnitTests
    {
        [global::TUnit.Core.Test]
        [global::TUnit.Core.Arguments(LuaCompatibilityVersion.Lua51)]
        [global::TUnit.Core.Arguments(LuaCompatibilityVersion.Lua52)]
        [global::TUnit.Core.Arguments(LuaCompatibilityVersion.Lua53)]
        [global::TUnit.Core.Arguments(LuaCompatibilityVersion.Lua54)]
        [global::TUnit.Core.Arguments(LuaCompatibilityVersion.Lua55)]
        public async Task TryCreateIfVisibleReturnsDescriptorForPublicProperty(
            LuaCompatibilityVersion version
        )
        {
            PropertyInfo property = SamplePropertiesMetadata.StaticValue;

            PropertyMemberDescriptor descriptor = PropertyMemberDescriptor.TryCreateIfVisible(
                property,
                InteropAccessMode.Reflection
            );

            await Assert.That(descriptor).IsNotNull();
            await Assert.That(descriptor.Name).IsEqualTo(nameof(SampleProperties.StaticValue));
            await Assert.That(descriptor.IsStatic).IsTrue();
            await Assert.That(descriptor.CanRead).IsTrue();
            await Assert.That(descriptor.CanWrite).IsTrue();
        }

        [global::TUnit.Core.Test]
        [global::TUnit.Core.Arguments(LuaCompatibilityVersion.Lua51)]
        [global::TUnit.Core.Arguments(LuaCompatibilityVersion.Lua52)]
        [global::TUnit.Core.Arguments(LuaCompatibilityVersion.Lua53)]
        [global::TUnit.Core.Arguments(LuaCompatibilityVersion.Lua54)]
        [global::TUnit.Core.Arguments(LuaCompatibilityVersion.Lua55)]
        public async Task TryCreateIfVisibleRespectsHiddenAttribute(LuaCompatibilityVersion version)
        {
            PropertyInfo property = SamplePropertiesMetadata.HiddenProperty;

            PropertyMemberDescriptor descriptor = PropertyMemberDescriptor.TryCreateIfVisible(
                property,
                InteropAccessMode.Reflection
            );

            await Assert.That(descriptor).IsNull();
        }

        [global::TUnit.Core.Test]
        [global::TUnit.Core.Arguments(LuaCompatibilityVersion.Lua51)]
        [global::TUnit.Core.Arguments(LuaCompatibilityVersion.Lua52)]
        [global::TUnit.Core.Arguments(LuaCompatibilityVersion.Lua53)]
        [global::TUnit.Core.Arguments(LuaCompatibilityVersion.Lua54)]
        [global::TUnit.Core.Arguments(LuaCompatibilityVersion.Lua55)]
        public async Task TryCreateIfVisibleHonorsAccessorVisibilityOverrides(
            LuaCompatibilityVersion version
        )
        {
            PropertyInfo property = SamplePropertiesMetadata.HiddenSetter;

            PropertyMemberDescriptor descriptor = PropertyMemberDescriptor.TryCreateIfVisible(
                property,
                InteropAccessMode.Reflection
            );

            await Assert.That(descriptor).IsNotNull();
            await Assert.That(descriptor.CanRead).IsTrue();
            await Assert.That(descriptor.CanWrite).IsFalse();
        }

        [global::TUnit.Core.Test]
        [global::TUnit.Core.Arguments(LuaCompatibilityVersion.Lua51)]
        [global::TUnit.Core.Arguments(LuaCompatibilityVersion.Lua52)]
        [global::TUnit.Core.Arguments(LuaCompatibilityVersion.Lua53)]
        [global::TUnit.Core.Arguments(LuaCompatibilityVersion.Lua54)]
        [global::TUnit.Core.Arguments(LuaCompatibilityVersion.Lua55)]
        public async Task TryCreateIfVisibleAllowsNonPublicPropertyMarkedVisible(
            LuaCompatibilityVersion version
        )
        {
            PropertyInfo property = SamplePropertiesMetadata.PrivateButVisible;

            PropertyMemberDescriptor descriptor = PropertyMemberDescriptor.TryCreateIfVisible(
                property,
                InteropAccessMode.Reflection
            );

            await Assert.That(descriptor).IsNotNull();
            await Assert.That(descriptor.CanRead).IsTrue();
            await Assert.That(descriptor.CanWrite).IsTrue();
        }

        [global::TUnit.Core.Test]
        [global::TUnit.Core.Arguments(LuaCompatibilityVersion.Lua51)]
        [global::TUnit.Core.Arguments(LuaCompatibilityVersion.Lua52)]
        [global::TUnit.Core.Arguments(LuaCompatibilityVersion.Lua53)]
        [global::TUnit.Core.Arguments(LuaCompatibilityVersion.Lua54)]
        [global::TUnit.Core.Arguments(LuaCompatibilityVersion.Lua55)]
        public async Task MemberAccessReflectsAvailableAccessors(LuaCompatibilityVersion version)
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

            await Assert
                .That(readWrite.MemberAccess)
                .IsEqualTo(MemberDescriptorAccess.CanRead | MemberDescriptorAccess.CanWrite);
            await Assert.That(readOnly.MemberAccess).IsEqualTo(MemberDescriptorAccess.CanRead);
            await Assert.That(writeOnly.MemberAccess).IsEqualTo(MemberDescriptorAccess.CanWrite);
        }

        [global::TUnit.Core.Test]
        [global::TUnit.Core.Arguments(LuaCompatibilityVersion.Lua51)]
        [global::TUnit.Core.Arguments(LuaCompatibilityVersion.Lua52)]
        [global::TUnit.Core.Arguments(LuaCompatibilityVersion.Lua53)]
        [global::TUnit.Core.Arguments(LuaCompatibilityVersion.Lua54)]
        [global::TUnit.Core.Arguments(LuaCompatibilityVersion.Lua55)]
        public async Task ConstructorThrowsWhenBothAccessorsMissing(LuaCompatibilityVersion version)
        {
            PropertyInfo property = SamplePropertiesMetadata.InstanceValue;

            ArgumentNullException exception = ExpectException<ArgumentNullException>(() =>
            {
                _ = new PropertyMemberDescriptor(
                    property,
                    InteropAccessMode.Reflection,
                    null,
                    null
                );
            });

            await Assert.That(exception.ParamName).IsNotNull();
        }

        [global::TUnit.Core.Test]
        [global::TUnit.Core.Arguments(LuaCompatibilityVersion.Lua51)]
        [global::TUnit.Core.Arguments(LuaCompatibilityVersion.Lua52)]
        [global::TUnit.Core.Arguments(LuaCompatibilityVersion.Lua53)]
        [global::TUnit.Core.Arguments(LuaCompatibilityVersion.Lua54)]
        [global::TUnit.Core.Arguments(LuaCompatibilityVersion.Lua55)]
        public async Task ConstructorThrowsWhenPropertyNull(LuaCompatibilityVersion version)
        {
            ArgumentNullException exception = ExpectException<ArgumentNullException>(() =>
            {
                _ = new PropertyMemberDescriptor(null, InteropAccessMode.Reflection, null, null);
            });

            await Assert.That(exception.ParamName).IsEqualTo("pi");
        }

        [global::TUnit.Core.Test]
        [global::TUnit.Core.Arguments(LuaCompatibilityVersion.Lua51)]
        [global::TUnit.Core.Arguments(LuaCompatibilityVersion.Lua52)]
        [global::TUnit.Core.Arguments(LuaCompatibilityVersion.Lua53)]
        [global::TUnit.Core.Arguments(LuaCompatibilityVersion.Lua54)]
        [global::TUnit.Core.Arguments(LuaCompatibilityVersion.Lua55)]
        public async Task ConstructorFallsBackToReflectionWhenPlatformIsAot(
            LuaCompatibilityVersion version
        )
        {
            using PlatformDetectorOverrideScope platformScope =
                PlatformDetectorOverrideScope.SetRunningOnAot(true);

            PropertyMemberDescriptor descriptor = new(
                SamplePropertiesMetadata.InstanceValue,
                InteropAccessMode.Preoptimized
            );

            await Assert.That(descriptor.AccessMode).IsEqualTo(InteropAccessMode.Reflection);
        }

        [global::TUnit.Core.Test]
        [global::TUnit.Core.Arguments(LuaCompatibilityVersion.Lua51)]
        [global::TUnit.Core.Arguments(LuaCompatibilityVersion.Lua52)]
        [global::TUnit.Core.Arguments(LuaCompatibilityVersion.Lua53)]
        [global::TUnit.Core.Arguments(LuaCompatibilityVersion.Lua54)]
        [global::TUnit.Core.Arguments(LuaCompatibilityVersion.Lua55)]
        public async Task GetValueReturnsInstanceValueWithLazyOptimization(
            LuaCompatibilityVersion version
        )
        {
            PropertyMemberDescriptor descriptor = new(
                SamplePropertiesMetadata.InstanceValue,
                InteropAccessMode.LazyOptimized
            );
            SampleProperties instance = new() { InstanceValue = 42 };

            DynValue value = descriptor.GetValue(CreateScript(), instance);

            await Assert.That(value.Type).IsEqualTo(DataType.Number);
            await Assert.That(value.Number).IsEqualTo(42d);
        }

        [global::TUnit.Core.Test]
        [global::TUnit.Core.Arguments(LuaCompatibilityVersion.Lua51)]
        [global::TUnit.Core.Arguments(LuaCompatibilityVersion.Lua52)]
        [global::TUnit.Core.Arguments(LuaCompatibilityVersion.Lua53)]
        [global::TUnit.Core.Arguments(LuaCompatibilityVersion.Lua54)]
        [global::TUnit.Core.Arguments(LuaCompatibilityVersion.Lua55)]
        public async Task GetValueThrowsWhenGetterMissing(LuaCompatibilityVersion version)
        {
            PropertyMemberDescriptor descriptor = PropertyMemberDescriptor.TryCreateIfVisible(
                SamplePropertiesMetadata.SetterOnly,
                InteropAccessMode.Reflection
            );

            ScriptRuntimeException exception = ExpectException<ScriptRuntimeException>(() =>
                descriptor.GetValue(CreateScript(), new SampleProperties())
            );

            await Assert.That(exception.Message).Contains("cannot be read");
        }

        [global::TUnit.Core.Test]
        [global::TUnit.Core.Arguments(LuaCompatibilityVersion.Lua51)]
        [global::TUnit.Core.Arguments(LuaCompatibilityVersion.Lua52)]
        [global::TUnit.Core.Arguments(LuaCompatibilityVersion.Lua53)]
        [global::TUnit.Core.Arguments(LuaCompatibilityVersion.Lua54)]
        [global::TUnit.Core.Arguments(LuaCompatibilityVersion.Lua55)]
        public async Task GetValueThrowsWhenInstanceMissing(LuaCompatibilityVersion version)
        {
            PropertyMemberDescriptor descriptor = new(
                SamplePropertiesMetadata.InstanceValue,
                InteropAccessMode.Reflection
            );

            ScriptRuntimeException exception = ExpectException<ScriptRuntimeException>(() =>
                descriptor.GetValue(CreateScript(), null)
            );

            await Assert.That(exception.Message).Contains("attempt to access instance member");
        }

        [global::TUnit.Core.Test]
        [global::TUnit.Core.Arguments(LuaCompatibilityVersion.Lua51)]
        [global::TUnit.Core.Arguments(LuaCompatibilityVersion.Lua52)]
        [global::TUnit.Core.Arguments(LuaCompatibilityVersion.Lua53)]
        [global::TUnit.Core.Arguments(LuaCompatibilityVersion.Lua54)]
        [global::TUnit.Core.Arguments(LuaCompatibilityVersion.Lua55)]
        public async Task SetValueUpdatesInstancePropertyAndConvertsNumbers(
            LuaCompatibilityVersion version
        )
        {
            PropertyMemberDescriptor descriptor = new(
                SamplePropertiesMetadata.InstanceValue,
                InteropAccessMode.Reflection
            );
            SampleProperties instance = new();

            descriptor.SetValue(CreateScript(), instance, DynValue.NewNumber(5.1));

            await Assert.That(instance.InstanceValue).IsEqualTo(5);
        }

        [global::TUnit.Core.Test]
        [global::TUnit.Core.Arguments(LuaCompatibilityVersion.Lua51)]
        [global::TUnit.Core.Arguments(LuaCompatibilityVersion.Lua52)]
        [global::TUnit.Core.Arguments(LuaCompatibilityVersion.Lua53)]
        [global::TUnit.Core.Arguments(LuaCompatibilityVersion.Lua54)]
        [global::TUnit.Core.Arguments(LuaCompatibilityVersion.Lua55)]
        public async Task SetValueThrowsWhenSetterMissing(LuaCompatibilityVersion version)
        {
            PropertyMemberDescriptor descriptor = PropertyMemberDescriptor.TryCreateIfVisible(
                SamplePropertiesMetadata.GetterOnly,
                InteropAccessMode.Reflection
            );

            ScriptRuntimeException exception = ExpectException<ScriptRuntimeException>(() =>
                descriptor.SetValue(CreateScript(), new SampleProperties(), DynValue.NewNumber(1))
            );

            await Assert.That(exception.Message).Contains("cannot be assigned");
        }

        [global::TUnit.Core.Test]
        [global::TUnit.Core.Arguments(LuaCompatibilityVersion.Lua51)]
        [global::TUnit.Core.Arguments(LuaCompatibilityVersion.Lua52)]
        [global::TUnit.Core.Arguments(LuaCompatibilityVersion.Lua53)]
        [global::TUnit.Core.Arguments(LuaCompatibilityVersion.Lua54)]
        [global::TUnit.Core.Arguments(LuaCompatibilityVersion.Lua55)]
        public async Task SetValueThrowsWhenDynValueNull(LuaCompatibilityVersion version)
        {
            PropertyMemberDescriptor descriptor = new(
                SamplePropertiesMetadata.InstanceValue,
                InteropAccessMode.Reflection
            );

            ArgumentNullException exception = ExpectException<ArgumentNullException>(() =>
                descriptor.SetValue(CreateScript(), new SampleProperties(), null)
            );

            await Assert.That(exception.ParamName).IsEqualTo("value");
        }

        [global::TUnit.Core.Test]
        [global::TUnit.Core.Arguments(LuaCompatibilityVersion.Lua51)]
        [global::TUnit.Core.Arguments(LuaCompatibilityVersion.Lua52)]
        [global::TUnit.Core.Arguments(LuaCompatibilityVersion.Lua53)]
        [global::TUnit.Core.Arguments(LuaCompatibilityVersion.Lua54)]
        [global::TUnit.Core.Arguments(LuaCompatibilityVersion.Lua55)]
        public async Task GetValueThrowsWhenScriptMissing(LuaCompatibilityVersion version)
        {
            PropertyMemberDescriptor descriptor = new(
                SamplePropertiesMetadata.InstanceValue,
                InteropAccessMode.Reflection
            );

            DynValue value = descriptor.GetValue(null, new SampleProperties { InstanceValue = 3 });
            await Assert.That(value.Number).IsEqualTo(3d);
        }

        [global::TUnit.Core.Test]
        [global::TUnit.Core.Arguments(LuaCompatibilityVersion.Lua51)]
        [global::TUnit.Core.Arguments(LuaCompatibilityVersion.Lua52)]
        [global::TUnit.Core.Arguments(LuaCompatibilityVersion.Lua53)]
        [global::TUnit.Core.Arguments(LuaCompatibilityVersion.Lua54)]
        [global::TUnit.Core.Arguments(LuaCompatibilityVersion.Lua55)]
        public async Task SetValueThrowsWhenScriptMissing(LuaCompatibilityVersion version)
        {
            PropertyMemberDescriptor descriptor = new(
                SamplePropertiesMetadata.InstanceValue,
                InteropAccessMode.Reflection
            );

            SampleProperties instance = new();
            descriptor.SetValue(null, instance, DynValue.NewNumber(1));
            await Assert.That(instance.InstanceValue).IsEqualTo(1);
        }

        [global::TUnit.Core.Test]
        [global::TUnit.Core.Arguments(LuaCompatibilityVersion.Lua51)]
        [global::TUnit.Core.Arguments(LuaCompatibilityVersion.Lua52)]
        [global::TUnit.Core.Arguments(LuaCompatibilityVersion.Lua53)]
        [global::TUnit.Core.Arguments(LuaCompatibilityVersion.Lua54)]
        [global::TUnit.Core.Arguments(LuaCompatibilityVersion.Lua55)]
        public async Task SetValueThrowsOnTypeMismatch(LuaCompatibilityVersion version)
        {
            PropertyMemberDescriptor descriptor = new(
                SamplePropertiesMetadata.InstanceValue,
                InteropAccessMode.Reflection
            );

            ScriptRuntimeException exception = ExpectException<ScriptRuntimeException>(() =>
                descriptor.SetValue(
                    CreateScript(),
                    new SampleProperties(),
                    DynValue.NewString("bad")
                )
            );

            await Assert.That(exception.Message).Contains("cannot convert a string");
        }

        [global::TUnit.Core.Test]
        [global::TUnit.Core.Arguments(LuaCompatibilityVersion.Lua51)]
        [global::TUnit.Core.Arguments(LuaCompatibilityVersion.Lua52)]
        [global::TUnit.Core.Arguments(LuaCompatibilityVersion.Lua53)]
        [global::TUnit.Core.Arguments(LuaCompatibilityVersion.Lua54)]
        [global::TUnit.Core.Arguments(LuaCompatibilityVersion.Lua55)]
        public async Task SetValueConvertsDoubleToPropertyType(LuaCompatibilityVersion version)
        {
            PropertyMemberDescriptor descriptor = PropertyMemberDescriptor.TryCreateIfVisible(
                SamplePropertiesMetadata.ShortValue,
                InteropAccessMode.Reflection
            );

            SampleProperties instance = new();
            descriptor.SetValue(CreateScript(), instance, DynValue.NewNumber(12));

            await Assert.That(instance.ShortValue).IsEqualTo((short)12);
        }

        [global::TUnit.Core.Test]
        [global::TUnit.Core.Arguments(LuaCompatibilityVersion.Lua51)]
        [global::TUnit.Core.Arguments(LuaCompatibilityVersion.Lua52)]
        [global::TUnit.Core.Arguments(LuaCompatibilityVersion.Lua53)]
        [global::TUnit.Core.Arguments(LuaCompatibilityVersion.Lua54)]
        [global::TUnit.Core.Arguments(LuaCompatibilityVersion.Lua55)]
        public async Task SetValueWrapsArgumentExceptionFromOptimizedSetter(
            LuaCompatibilityVersion version
        )
        {
            PropertyMemberDescriptor descriptor = new(
                SamplePropertiesMetadata.InstanceValue,
                InteropAccessMode.Reflection
            );
            OverrideOptimizedSetter(
                descriptor,
                (_, _) => throw new ArgumentException("failing reflection assignment")
            );

            ScriptRuntimeException exception = ExpectException<ScriptRuntimeException>(() =>
                descriptor.SetValue(CreateScript(), new SampleProperties(), DynValue.NewNumber(1))
            );

            await Assert.That(exception.Message).Contains("cannot find a conversion");
        }

        [global::TUnit.Core.Test]
        [global::TUnit.Core.Arguments(LuaCompatibilityVersion.Lua51)]
        [global::TUnit.Core.Arguments(LuaCompatibilityVersion.Lua52)]
        [global::TUnit.Core.Arguments(LuaCompatibilityVersion.Lua53)]
        [global::TUnit.Core.Arguments(LuaCompatibilityVersion.Lua54)]
        [global::TUnit.Core.Arguments(LuaCompatibilityVersion.Lua55)]
        public async Task SetValueWrapsInvalidCastExceptionFromOptimizedSetter(
            LuaCompatibilityVersion version
        )
        {
            PropertyMemberDescriptor descriptor = new(
                SamplePropertiesMetadata.InstanceValue,
                InteropAccessMode.Reflection
            );
            OverrideOptimizedSetter(
                descriptor,
                (_, _) => throw new InvalidCastException("failing optimized assignment")
            );

            ScriptRuntimeException exception = ExpectException<ScriptRuntimeException>(() =>
                descriptor.SetValue(CreateScript(), new SampleProperties(), DynValue.NewNumber(1))
            );

            await Assert.That(exception.Message).Contains("cannot find a conversion");
        }

        [global::TUnit.Core.Test]
        [global::TUnit.Core.Arguments(LuaCompatibilityVersion.Lua51)]
        [global::TUnit.Core.Arguments(LuaCompatibilityVersion.Lua52)]
        [global::TUnit.Core.Arguments(LuaCompatibilityVersion.Lua53)]
        [global::TUnit.Core.Arguments(LuaCompatibilityVersion.Lua54)]
        [global::TUnit.Core.Arguments(LuaCompatibilityVersion.Lua55)]
        public async Task SetValueThrowsWhenInstanceMissing(LuaCompatibilityVersion version)
        {
            PropertyMemberDescriptor descriptor = new(
                SamplePropertiesMetadata.InstanceValue,
                InteropAccessMode.Reflection
            );

            ScriptRuntimeException exception = ExpectException<ScriptRuntimeException>(() =>
                descriptor.SetValue(CreateScript(), null, DynValue.NewNumber(1))
            );

            await Assert.That(exception.Message).Contains("attempt to access instance member");
        }

        [global::TUnit.Core.Test]
        [global::TUnit.Core.Arguments(LuaCompatibilityVersion.Lua51)]
        [global::TUnit.Core.Arguments(LuaCompatibilityVersion.Lua52)]
        [global::TUnit.Core.Arguments(LuaCompatibilityVersion.Lua53)]
        [global::TUnit.Core.Arguments(LuaCompatibilityVersion.Lua54)]
        [global::TUnit.Core.Arguments(LuaCompatibilityVersion.Lua55)]
        public async Task SetValueOptimizesStaticSetterWhenLazy(LuaCompatibilityVersion version)
        {
            SampleProperties.LazyStatic = 0;
            PropertyMemberDescriptor descriptor = new(
                SamplePropertiesMetadata.LazyStatic,
                InteropAccessMode.LazyOptimized
            );

            descriptor.SetValue(CreateScript(), null, DynValue.NewNumber(9));

            await Assert.That(SampleProperties.LazyStatic).IsEqualTo(9);
            SampleProperties.LazyStatic = 0;
        }

        [global::TUnit.Core.Test]
        [global::TUnit.Core.Arguments(LuaCompatibilityVersion.Lua51)]
        [global::TUnit.Core.Arguments(LuaCompatibilityVersion.Lua52)]
        [global::TUnit.Core.Arguments(LuaCompatibilityVersion.Lua53)]
        [global::TUnit.Core.Arguments(LuaCompatibilityVersion.Lua54)]
        [global::TUnit.Core.Arguments(LuaCompatibilityVersion.Lua55)]
        public async Task OptimizePrecompilesGetterAndSetter(LuaCompatibilityVersion version)
        {
            PropertyMemberDescriptor descriptor = new(
                SamplePropertiesMetadata.InstanceValue,
                InteropAccessMode.LazyOptimized
            );
            SampleProperties instance = new() { InstanceValue = 12 };

            ((IOptimizableDescriptor)descriptor).Optimize();

            DynValue value = descriptor.GetValue(CreateScript(), instance);
            await Assert.That(value.Number).IsEqualTo(12d);

            descriptor.SetValue(CreateScript(), instance, DynValue.NewNumber(4));
            await Assert.That(instance.InstanceValue).IsEqualTo(4);
        }

        [global::TUnit.Core.Test]
        [global::TUnit.Core.Arguments(LuaCompatibilityVersion.Lua51)]
        [global::TUnit.Core.Arguments(LuaCompatibilityVersion.Lua52)]
        [global::TUnit.Core.Arguments(LuaCompatibilityVersion.Lua53)]
        [global::TUnit.Core.Arguments(LuaCompatibilityVersion.Lua54)]
        [global::TUnit.Core.Arguments(LuaCompatibilityVersion.Lua55)]
        public async Task PrepareForWiringPopulatesMetadataTable(LuaCompatibilityVersion version)
        {
            PropertyMemberDescriptor descriptor = new(
                SamplePropertiesMetadata.InstanceValue,
                InteropAccessMode.Reflection
            );
            Table metadata = new(CreateScript());

            descriptor.PrepareForWiring(metadata);

            await Assert
                .That(metadata.Get("name").String)
                .IsEqualTo(nameof(SampleProperties.InstanceValue));
            await Assert.That(metadata.Get("static").Boolean).IsFalse();
            await Assert.That(metadata.Get("read").Boolean).IsTrue();
            await Assert.That(metadata.Get("write").Boolean).IsTrue();
            await Assert.That(metadata.Get("declvtype").Boolean).IsFalse();
        }

        [global::TUnit.Core.Test]
        [global::TUnit.Core.Arguments(LuaCompatibilityVersion.Lua51)]
        [global::TUnit.Core.Arguments(LuaCompatibilityVersion.Lua52)]
        [global::TUnit.Core.Arguments(LuaCompatibilityVersion.Lua53)]
        [global::TUnit.Core.Arguments(LuaCompatibilityVersion.Lua54)]
        [global::TUnit.Core.Arguments(LuaCompatibilityVersion.Lua55)]
        public async Task PrepareForWiringThrowsWhenTableNull(LuaCompatibilityVersion version)
        {
            PropertyMemberDescriptor descriptor = new(
                SamplePropertiesMetadata.InstanceValue,
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

        private static void OverrideOptimizedSetter(
            PropertyMemberDescriptor descriptor,
            Action<object, object> setter
        )
        {
            PropertyMemberDescriptor.TestHooks.SetOptimizedSetter(descriptor, setter);
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

        private static class SamplePropertiesMetadata
        {
            internal static PropertyInfo StaticValue { get; } =
                typeof(SampleProperties).GetProperty(nameof(SampleProperties.StaticValue));

            internal static PropertyInfo LazyStatic { get; } =
                typeof(SampleProperties).GetProperty(nameof(SampleProperties.LazyStatic));

            internal static PropertyInfo InstanceValue { get; } =
                typeof(SampleProperties).GetProperty(nameof(SampleProperties.InstanceValue));

            internal static PropertyInfo SetterOnly { get; } =
                typeof(SampleProperties).GetProperty(nameof(SampleProperties.SetterOnly));

            internal static PropertyInfo GetterOnly { get; } =
                typeof(SampleProperties).GetProperty(nameof(SampleProperties.GetterOnly));

            internal static PropertyInfo ShortValue { get; } =
                typeof(SampleProperties).GetProperty(nameof(SampleProperties.ShortValue));

            internal static PropertyInfo HiddenProperty { get; } =
                typeof(SampleProperties).GetProperty(nameof(SampleProperties.HiddenProperty));

            internal static PropertyInfo HiddenSetter { get; } =
                typeof(SampleProperties).GetProperty(nameof(SampleProperties.HiddenSetter));

            internal static PropertyInfo PrivateButVisible { get; } =
                SampleProperties.PrivateButVisibleProperty;
        }
    }
}
