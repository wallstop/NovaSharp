namespace WallstopStudios.NovaSharp.Interpreter.Tests.TUnit.Units.Interop.Descriptors
{
    using System;
    using System.Collections.Generic;
    using System.Diagnostics.CodeAnalysis;
    using System.IO;
    using System.Linq;
    using System.Linq.Expressions;
    using System.Reflection;
    using System.Reflection.Emit;
    using System.Threading.Tasks;
    using global::TUnit.Assertions;
    using WallstopStudios.NovaSharp.Interpreter.Interop;
    using WallstopStudios.NovaSharp.Interpreter.Interop.Attributes;

    public sealed class DescriptorHelpersTests
    {
        private static readonly string[] ExpectedMetaNames = { "__index", "__len" };

        static DescriptorHelpersTests()
        {
            _ = new VisibilityTargets();
            _ = new DescriptorHelpersPublicType();
            _ = new VisibilityFixtures.PublicNested();
            _ = new VisibilityFixtures.ProtectedInternalNested();
            _ = Activator.CreateInstance(
                VisibilityFixtures.Metadata.ProtectedType,
                nonPublic: true
            );
            _ = new VisibilityFixtures.GenericHost<string>();
            _ = new VisibilityFixtures.GenericHost<string>.InnerType();
            _ = new MetaFixtures();
            _ = new DerivedSample();
        }

        [global::TUnit.Core.Test]
        public async Task GetVisibilityFromAttributesHandlesNullAndExplicitAttributes()
        {
            bool? defaultVisibility = DescriptorHelpers.GetVisibilityFromAttributes(null);
            await Assert
                .That(defaultVisibility.GetValueOrDefault())
                .IsFalse()
                .ConfigureAwait(false);

            MemberInfo visible = VisibilityTargets.Metadata.VisibleMember;
            MemberInfo hidden = VisibilityTargets.Metadata.HiddenMember;
            MemberInfo overridden = VisibilityTargets.Metadata.ForcedHiddenMember;

            await Assert.That(visible.GetVisibilityFromAttributes()).IsTrue().ConfigureAwait(false);
            await Assert.That(hidden.GetVisibilityFromAttributes()).IsFalse().ConfigureAwait(false);
            await Assert
                .That(overridden.GetVisibilityFromAttributes())
                .IsFalse()
                .ConfigureAwait(false);
        }

        [global::TUnit.Core.Test]
        public async Task GetVisibilityFromAttributesThrowsWhenVisibleAttributeDuplicated()
        {
            MemberInfo member = new FakeMemberInfo(
                new NovaSharpVisibleAttribute(true),
                new NovaSharpVisibleAttribute(true)
            );

            InvalidOperationException exception = Assert.Throws<InvalidOperationException>(() =>
                member.GetVisibilityFromAttributes()
            );

            await Assert.That(exception.Message).Contains("VisibleAttribute").ConfigureAwait(false);
        }

        [global::TUnit.Core.Test]
        public async Task GetVisibilityFromAttributesThrowsWhenHiddenAttributeDuplicated()
        {
            MemberInfo member = new FakeMemberInfo(
                new NovaSharpHiddenAttribute(),
                new NovaSharpHiddenAttribute()
            );

            InvalidOperationException exception = Assert.Throws<InvalidOperationException>(() =>
                member.GetVisibilityFromAttributes()
            );

            await Assert.That(exception.Message).Contains("HiddenAttribute").ConfigureAwait(false);
        }

        [global::TUnit.Core.Test]
        public async Task GetVisibilityFromAttributesThrowsWhenAttributesConflict()
        {
            MemberInfo conflicting = VisibilityTargets.Metadata.ConflictingMember;

            InvalidOperationException exception = Assert.Throws<InvalidOperationException>(() =>
                conflicting.GetVisibilityFromAttributes()
            );

            await Assert.That(exception.Message).Contains("discording").ConfigureAwait(false);
        }

        [global::TUnit.Core.Test]
        public async Task DescriptorVisibilityHelpersValidateNullArguments()
        {
            FieldInfo field = MemberVisibilityFixtures.Metadata.PublicField;
            PropertyInfo property = PropertyFixtures.Metadata.GetterOnly;
            MethodInfo method = MemberVisibilityFixtures.Metadata.PublicMethod;

            ArgumentNullException typeException = Assert.Throws<ArgumentNullException>(() =>
                DescriptorHelpers.GetClrVisibility((Type)null)
            );
            await Assert.That(typeException.ParamName).IsEqualTo("type").ConfigureAwait(false);

            ArgumentNullException fieldException = Assert.Throws<ArgumentNullException>(() =>
                DescriptorHelpers.GetClrVisibility((FieldInfo)null)
            );
            await Assert.That(fieldException.ParamName).IsEqualTo("info").ConfigureAwait(false);

            ArgumentNullException propertyException = Assert.Throws<ArgumentNullException>(() =>
                DescriptorHelpers.GetClrVisibility((PropertyInfo)null)
            );
            await Assert.That(propertyException.ParamName).IsEqualTo("info").ConfigureAwait(false);

            ArgumentNullException methodBaseException = Assert.Throws<ArgumentNullException>(() =>
                DescriptorHelpers.GetClrVisibility((MethodBase)null)
            );
            await Assert
                .That(methodBaseException.ParamName)
                .IsEqualTo("info")
                .ConfigureAwait(false);

            ArgumentNullException propertyInfoException = Assert.Throws<ArgumentNullException>(() =>
                DescriptorHelpers.IsPropertyInfoPublic(null)
            );
            await Assert
                .That(propertyInfoException.ParamName)
                .IsEqualTo("pi")
                .ConfigureAwait(false);

            ArgumentNullException metaException = Assert.Throws<ArgumentNullException>(() =>
                DescriptorHelpers.GetMetaNamesFromAttributes(null)
            );
            await Assert.That(metaException.ParamName).IsEqualTo("mi").ConfigureAwait(false);

            ArgumentNullException conversionException = Assert.Throws<ArgumentNullException>(() =>
                DescriptorHelpers.GetConversionMethodName(null)
            );
            await Assert
                .That(conversionException.ParamName)
                .IsEqualTo("type")
                .ConfigureAwait(false);

            await Assert.That(field.GetClrVisibility()).IsEqualTo("public").ConfigureAwait(false);
            await Assert.That(property.IsPropertyInfoPublic()).IsTrue().ConfigureAwait(false);
            await Assert.That(method.GetClrVisibility()).IsEqualTo("public").ConfigureAwait(false);
        }

        [global::TUnit.Core.Test]
        public async Task GetClrVisibilityReturnsExpectedValuesForTypes()
        {
            Type protectedInternal = VisibilityFixtures.Metadata.ProtectedInternalType;
            Type protectedNested = VisibilityFixtures.Metadata.ProtectedType;
            Type privateNested = VisibilityFixtures.Metadata.PrivateType;

            await Assert
                .That(typeof(DescriptorHelpersPublicType).GetClrVisibility())
                .IsEqualTo("public")
                .ConfigureAwait(false);
            await Assert
                .That(typeof(DescriptorHelpersTestsInternalTopLevel).GetClrVisibility())
                .IsEqualTo("internal")
                .ConfigureAwait(false);
            await Assert
                .That(protectedInternal!.GetClrVisibility())
                .IsEqualTo("protected-internal")
                .ConfigureAwait(false);
            await Assert
                .That(protectedNested!.GetClrVisibility())
                .IsEqualTo("protected")
                .ConfigureAwait(false);
            await Assert
                .That(privateNested!.GetClrVisibility())
                .IsEqualTo("private")
                .ConfigureAwait(false);
        }

        [global::TUnit.Core.Test]
        public async Task GetClrVisibilityReturnsExpectedValuesForMembers()
        {
            FieldInfo publicField = MemberVisibilityFixtures.Metadata.PublicField;
            FieldInfo internalField = MemberVisibilityFixtures.Metadata.InternalField;
            FieldInfo protectedField = MemberVisibilityFixtures.Metadata.ProtectedField;
            FieldInfo protectedInternalField = MemberVisibilityFixtures
                .Metadata
                .ProtectedInternalField;
            MethodInfo privateMethod = MemberVisibilityFixtures.Metadata.PrivateMethod;
            MethodInfo publicMethod = MemberVisibilityFixtures.Metadata.PublicMethod;
            MethodInfo protectedInternalMethod = MemberVisibilityFixtures
                .Metadata
                .ProtectedInternalMethod;

            await Assert
                .That(publicField.GetClrVisibility())
                .IsEqualTo("public")
                .ConfigureAwait(false);
            await Assert
                .That(internalField.GetClrVisibility())
                .IsEqualTo("internal")
                .ConfigureAwait(false);
            await Assert
                .That(protectedField.GetClrVisibility())
                .IsEqualTo("protected")
                .ConfigureAwait(false);
            await Assert
                .That(protectedInternalField.GetClrVisibility())
                .IsEqualTo("protected-internal")
                .ConfigureAwait(false);
            await Assert
                .That(privateMethod.GetClrVisibility())
                .IsEqualTo("private")
                .ConfigureAwait(false);
            await Assert
                .That(publicMethod.GetClrVisibility())
                .IsEqualTo("public")
                .ConfigureAwait(false);
            await Assert
                .That(protectedInternalMethod.GetClrVisibility())
                .IsEqualTo("protected-internal")
                .ConfigureAwait(false);
        }

        [global::TUnit.Core.Test]
        public async Task GetClrVisibilityReturnsExpectedValuesForProperties()
        {
            PropertyInfo publicProperty = PropertyFixtures.Metadata.GetterOnly;
            PropertyInfo internalProperty = PropertyFixtures.Metadata.InternalAccessors;
            PropertyInfo protectedProperty = PropertyFixtures.Metadata.ProtectedGetterOnly;

            await Assert
                .That(publicProperty.GetClrVisibility())
                .IsEqualTo("public")
                .ConfigureAwait(false);
            await Assert
                .That(internalProperty.GetClrVisibility())
                .IsEqualTo("internal")
                .ConfigureAwait(false);
            await Assert
                .That(protectedProperty.GetClrVisibility())
                .IsEqualTo("protected")
                .ConfigureAwait(false);
        }

        [global::TUnit.Core.Test]
        public async Task IsPropertyInfoPublicConsidersGetterOrSetter()
        {
            PropertyInfo getterOnly = PropertyFixtures.Metadata.GetterOnly;
            PropertyInfo setterOnly = PropertyFixtures.Metadata.SetterOnly;
            PropertyInfo privateProperty = PropertyFixtures.Metadata.PrivateBoth;

            await Assert.That(getterOnly.IsPropertyInfoPublic()).IsTrue().ConfigureAwait(false);
            await Assert.That(setterOnly.IsPropertyInfoPublic()).IsTrue().ConfigureAwait(false);
            await Assert
                .That(privateProperty.IsPropertyInfoPublic())
                .IsFalse()
                .ConfigureAwait(false);
        }

        [global::TUnit.Core.Test]
        public async Task GetMetaNamesFromAttributesReturnsDeclaredNames()
        {
            MethodInfo method = MetaFixtures.Metadata.Metamethods;

            IReadOnlyList<string> names = method.GetMetaNamesFromAttributes();

            await Assert.That(names).IsEquivalentTo(ExpectedMetaNames).ConfigureAwait(false);
        }

        [global::TUnit.Core.Test]
        public async Task GetMetaNamesFromAttributesReturnsEmptyWhenNoAttributesPresent()
        {
            MethodInfo method = MetaFixtures.Metadata.PlainMethod;

            IReadOnlyList<string> names = method.GetMetaNamesFromAttributes();

            await Assert.That((IEnumerable<string>)names).IsEmpty().ConfigureAwait(false);
        }

        [global::TUnit.Core.Test]
        public async Task GetConversionMethodNameSanitizesNonAlphaNumericCharacters()
        {
            string name =
                typeof(VisibilityFixtures.GenericHost<string>.InnerType).GetConversionMethodName();

            await Assert.That(name).IsEqualTo("__toInnerType").ConfigureAwait(false);

            string genericName = typeof(VisibilityFixtures.GenericHost<>).GetConversionMethodName();
            await Assert.That(genericName).IsEqualTo("__toGenericHost_1").ConfigureAwait(false);
        }

        [global::TUnit.Core.Test]
        public async Task IsDelegateTypeIdentifiesDelegates()
        {
            await Assert.That(typeof(Action).IsDelegateType()).IsTrue().ConfigureAwait(false);
            await Assert.That(typeof(string).IsDelegateType()).IsFalse().ConfigureAwait(false);
        }

        [global::TUnit.Core.Test]
        public async Task GetAllImplementedTypesIncludesBaseTypesAndInterfaces()
        {
            Type[] types = typeof(DerivedSample).GetAllImplementedTypes().ToArray();

            await Assert.That(types.Contains(typeof(DerivedSample))).IsTrue().ConfigureAwait(false);
            await Assert.That(types.Contains(typeof(BaseSample))).IsTrue().ConfigureAwait(false);
            await Assert
                .That(types.Contains(typeof(ISampleInterface)))
                .IsTrue()
                .ConfigureAwait(false);
        }

        [global::TUnit.Core.Test]
        public async Task SafeGetTypesReturnsEmptyArrayWhenAssemblyFailsToLoadTypes()
        {
            Assembly throwingAssembly = new ThrowingAssembly();

            Type[] types = throwingAssembly.SafeGetTypes();

            await Assert.That(types).IsEmpty().ConfigureAwait(false);
        }

        [global::TUnit.Core.Test]
        public async Task IdentifierHelpersValidateAndNormalizeNames()
        {
            await Assert
                .That(DescriptorHelpers.IsValidSimpleIdentifier("value_1"))
                .IsTrue()
                .ConfigureAwait(false);
            await Assert
                .That(DescriptorHelpers.IsValidSimpleIdentifier("1value"))
                .IsFalse()
                .ConfigureAwait(false);
            await Assert
                .That(DescriptorHelpers.IsValidSimpleIdentifier("value-1"))
                .IsFalse()
                .ConfigureAwait(false);
            await Assert
                .That(DescriptorHelpers.ToValidSimpleIdentifier("1 invalid-name"))
                .IsEqualTo("_1_invalid_name")
                .ConfigureAwait(false);
            await Assert
                .That(DescriptorHelpers.IsValidSimpleIdentifier(null))
                .IsFalse()
                .ConfigureAwait(false);
            await Assert
                .That(DescriptorHelpers.ToValidSimpleIdentifier(null))
                .IsEqualTo("_")
                .ConfigureAwait(false);
            await Assert
                .That(DescriptorHelpers.ToValidSimpleIdentifier(string.Empty))
                .IsEqualTo("_")
                .ConfigureAwait(false);
        }

        [global::TUnit.Core.Test]
        public async Task NameTransformationsCoverCamelCaseAndSnakeCaseVariants()
        {
            await Assert
                .That(DescriptorHelpers.Camelify("my_sample_value"))
                .IsEqualTo("mySampleValue")
                .ConfigureAwait(false);
            await Assert
                .That(DescriptorHelpers.Camelify("__Already__Mixed__"))
                .IsEqualTo("alreadyMixed")
                .ConfigureAwait(false);
            await Assert
                .That(DescriptorHelpers.Camelify("___"))
                .IsEqualTo(string.Empty)
                .ConfigureAwait(false);
            await Assert
                .That(DescriptorHelpers.ToUpperUnderscore("HttpRequestV2"))
                .IsEqualTo("HTTP_REQUEST_V2")
                .ConfigureAwait(false);
            await Assert
                .That(DescriptorHelpers.ToUpperUnderscore("Version42Beta"))
                .IsEqualTo("VERSION_42_BETA")
                .ConfigureAwait(false);
            await Assert
                .That(DescriptorHelpers.ToUpperUnderscore("value-with-dash"))
                .IsEqualTo("VALUE_WITH_DASH")
                .ConfigureAwait(false);
            await Assert
                .That(DescriptorHelpers.ToUpperUnderscore("Value123Name"))
                .IsEqualTo("VALUE_123_NAME")
                .ConfigureAwait(false);
            await Assert
                .That(DescriptorHelpers.ToUpperUnderscore("Value123"))
                .IsEqualTo("VALUE123")
                .ConfigureAwait(false);
            await Assert
                .That(DescriptorHelpers.UpperFirstLetter("sample"))
                .IsEqualTo("Sample")
                .ConfigureAwait(false);
            await Assert
                .That(DescriptorHelpers.NormalizeUppercaseRuns("HTTPRequestURL"))
                .IsEqualTo("HttpRequestUrl")
                .ConfigureAwait(false);
            await Assert
                .That(DescriptorHelpers.NormalizeUppercaseRuns("GPUFPSCounter"))
                .IsEqualTo("GpufpsCounter")
                .ConfigureAwait(false);
            await Assert
                .That(DescriptorHelpers.NormalizeUppercaseRuns(string.Empty))
                .IsEqualTo(string.Empty)
                .ConfigureAwait(false);
            await Assert
                .That(DescriptorHelpers.NormalizeUppercaseRuns("___"))
                .IsEqualTo(string.Empty)
                .ConfigureAwait(false);
            await Assert
                .That(DescriptorHelpers.NormalizeUppercaseRuns(null))
                .IsNull()
                .ConfigureAwait(false);
            await Assert
                .That(DescriptorHelpers.ToUpperUnderscore(null))
                .IsNull()
                .ConfigureAwait(false);

            ArgumentNullException exception = Assert.Throws<ArgumentNullException>(() =>
                DescriptorHelpers.Camelify(null)
            );
            await Assert.That(exception.ParamName).IsEqualTo("name").ConfigureAwait(false);
        }

        private sealed class VisibilityTargets
        {
            private int _invocationCount;

            private void TouchInstance()
            {
                _invocationCount++;
            }

            [NovaSharpVisible(true)]
            public void VisibleMember()
            {
                TouchInstance();
            }

            [NovaSharpHidden]
            public void HiddenMember()
            {
                TouchInstance();
            }

            [NovaSharpVisible(false)]
            public void ForcedHidden()
            {
                TouchInstance();
            }

            [NovaSharpVisible(true)]
            [NovaSharpHidden]
            public void ConflictingMember()
            {
                TouchInstance();
            }

            internal static class Metadata
            {
                internal static MethodInfo VisibleMember { get; } =
                    typeof(VisibilityTargets).GetMethod(nameof(VisibleMember))!;

                internal static MethodInfo HiddenMember { get; } =
                    typeof(VisibilityTargets).GetMethod(nameof(HiddenMember))!;

                internal static MethodInfo ForcedHiddenMember { get; } =
                    typeof(VisibilityTargets).GetMethod(nameof(ForcedHidden))!;

                internal static MethodInfo ConflictingMember { get; } =
                    typeof(VisibilityTargets).GetMethod(nameof(ConflictingMember))!;
            }
        }

        private static class VisibilityFixtures
        {
            static VisibilityFixtures()
            {
                _ = new PrivateNested();
                _ = new ProtectedNested();
            }

            public sealed class PublicNested { }

#pragma warning disable CS0628 // New protected member declared in sealed type
            protected internal sealed class ProtectedInternalNested { }
#pragma warning restore CS0628 // New protected member declared in sealed type

#pragma warning disable CS0628 // New protected member declared in sealed type
            protected sealed class ProtectedNested { }
#pragma warning restore CS0628 // New protected member declared in sealed type

            private sealed class PrivateNested { }

            public sealed class GenericHost<T>
            {
                private readonly string _instanceLabel;

                public GenericHost()
                {
                    _instanceLabel = typeof(T).Name;
                }

                internal string InstanceLabel => _instanceLabel;

                public sealed class InnerType { }
            }

            internal static class Metadata
            {
                internal static Type ProtectedInternalType => typeof(ProtectedInternalNested);

                internal static Type ProtectedType => typeof(ProtectedNested);

                internal static Type PrivateType => typeof(PrivateNested);
            }
        }

        private abstract class MemberVisibilityFixtures
        {
            private int _invocationCount;

            private void TouchInstance()
            {
                _invocationCount++;
            }

            public void PublicMethod()
            {
                TouchInstance();
            }

            private void PrivateMethod()
            {
                TouchInstance();
            }

            protected internal void ProtectedInternalMethod()
            {
                TouchInstance();
            }

            internal static class Metadata
            {
                internal static FieldInfo PublicField { get; } =
                    FieldVisibilityFixtures.PublicField;

                internal static FieldInfo InternalField { get; } =
                    FieldVisibilityFixtures.InternalField;

                internal static FieldInfo ProtectedField { get; } =
                    FieldVisibilityFixtures.ProtectedField;

                internal static FieldInfo ProtectedInternalField { get; } =
                    FieldVisibilityFixtures.ProtectedInternalField;

                internal static MethodInfo PrivateMethod { get; } =
                    GetInstanceMethod(f => f.PrivateMethod());

                internal static MethodInfo PublicMethod { get; } =
                    typeof(MemberVisibilityFixtures).GetMethod(nameof(PublicMethod))!;

                internal static MethodInfo ProtectedInternalMethod { get; } =
                    GetInstanceMethod(f => f.ProtectedInternalMethod());

                private static MethodInfo GetInstanceMethod(
                    Expression<Action<MemberVisibilityFixtures>> call
                )
                {
                    return ((MethodCallExpression)call.Body).Method;
                }

                private static MemberExpression GetMemberExpression(Expression expression)
                {
                    if (expression is MemberExpression member)
                    {
                        return member;
                    }

                    if (
                        expression is UnaryExpression unary
                        && unary.NodeType == ExpressionType.Convert
                        && unary.Operand is MemberExpression unaryMember
                    )
                    {
                        return unaryMember;
                    }

                    throw new InvalidOperationException("Expected member expression.");
                }
            }
        }

        private static class FieldVisibilityFixtures
        {
            private const string AssemblyNameValue = "DescriptorHelpersFieldFixtures";
            private const string ModuleName = "FieldVisibilityFixtures";
            private const string TypeName = "DescriptorHelpers.FieldVisibilityHost";
            private const string PublicFieldName = "PublicField";
            private const string InternalFieldName = "InternalField";
            private const string ProtectedFieldName = "ProtectedField";
            private const string ProtectedInternalFieldName = "ProtectedInternalField";

            private static readonly Type FieldHostType = CreateFieldHostType();

            internal static readonly FieldInfo PublicField = FieldHostType.GetField(
                PublicFieldName,
                BindingFlags.Public | BindingFlags.Static
            )!;

            internal static readonly FieldInfo InternalField = FieldHostType.GetField(
                InternalFieldName,
                BindingFlags.NonPublic | BindingFlags.Static
            )!;

            internal static readonly FieldInfo ProtectedField = FieldHostType.GetField(
                ProtectedFieldName,
                BindingFlags.NonPublic | BindingFlags.Static
            )!;

            internal static readonly FieldInfo ProtectedInternalField = FieldHostType.GetField(
                ProtectedInternalFieldName,
                BindingFlags.NonPublic | BindingFlags.Static
            )!;

            private static Type CreateFieldHostType()
            {
                AssemblyName name = new(AssemblyNameValue);
                AssemblyBuilder assembly = AssemblyBuilder.DefineDynamicAssembly(
                    name,
                    AssemblyBuilderAccess.Run
                );
                ModuleBuilder module = assembly.DefineDynamicModule(ModuleName);
                TypeBuilder builder = module.DefineType(
                    TypeName,
                    TypeAttributes.Class | TypeAttributes.NotPublic
                );

                DefineField(builder, PublicFieldName, FieldAttributes.Public);
                DefineField(builder, InternalFieldName, FieldAttributes.Assembly);
                DefineField(builder, ProtectedFieldName, FieldAttributes.Family);
                DefineField(builder, ProtectedInternalFieldName, FieldAttributes.FamORAssem);

                return builder.CreateTypeInfo()!.AsType();
            }

            private static void DefineField(
                TypeBuilder builder,
                string fieldName,
                FieldAttributes visibility
            )
            {
                builder.DefineField(fieldName, typeof(int), visibility | FieldAttributes.Static);
            }
        }

        private abstract class PropertyFixtures
        {
            // ReSharper disable once UnusedMember.Local
            public const string PrivatePropertyName = nameof(PrivateBoth);

            public int GetterOnly { get; private set; }

            public int SetterOnly { private get; set; }

            internal int InternalAccessors { get; set; }

            protected int ProtectedGetterOnly { get; private set; }

            private int PrivateBoth { get; set; }

            internal static class Metadata
            {
                internal static PropertyInfo GetterOnly { get; } =
                    typeof(PropertyFixtures).GetProperty(nameof(PropertyFixtures.GetterOnly))!;

                internal static PropertyInfo SetterOnly { get; } =
                    typeof(PropertyFixtures).GetProperty(nameof(PropertyFixtures.SetterOnly))!;

                internal static PropertyInfo InternalAccessors { get; } =
                    GetInstanceProperty(p => p.InternalAccessors);

                internal static PropertyInfo ProtectedGetterOnly { get; } =
                    GetInstanceProperty(p => p.ProtectedGetterOnly);

                internal static PropertyInfo PrivateBoth { get; } =
                    GetInstanceProperty(p => p.PrivateBoth);

                private static PropertyInfo GetInstanceProperty<TValue>(
                    Expression<Func<PropertyFixtures, TValue>> accessor
                )
                {
                    return (PropertyInfo)GetMemberExpression(accessor.Body).Member;
                }

                private static MemberExpression GetMemberExpression(Expression expression)
                {
                    if (expression is MemberExpression member)
                    {
                        return member;
                    }

                    if (
                        expression is UnaryExpression unary
                        && unary.NodeType == ExpressionType.Convert
                        && unary.Operand is MemberExpression unaryMember
                    )
                    {
                        return unaryMember;
                    }

                    throw new InvalidOperationException("Expected member expression.");
                }
            }
        }

        private sealed class MetaFixtures
        {
            private int _invocationCount;

            [NovaSharpUserDataMetamethod("__index")]
            [NovaSharpUserDataMetamethod("__len")]
            public void Metamethods()
            {
                _invocationCount++;
            }

            public void PlainMethod()
            {
                _invocationCount++;
            }

            internal static class Metadata
            {
                internal static MethodInfo Metamethods { get; } =
                    typeof(MetaFixtures).GetMethod(nameof(Metamethods))!;

                internal static MethodInfo PlainMethod { get; } =
                    typeof(MetaFixtures).GetMethod(nameof(PlainMethod))!;
            }
        }

        private sealed class FakeMemberInfo : MemberInfo
        {
            private readonly object[] _attributes;

            internal FakeMemberInfo(params object[] attributes)
            {
                _attributes = attributes ?? Array.Empty<object>();
            }

            public override object[] GetCustomAttributes(Type attributeType, bool inherit)
            {
                ArgumentNullException.ThrowIfNull(attributeType);

                if (_attributes.Length == 0)
                {
                    return Array.Empty<object>();
                }

                List<object> matches = new();

                for (int i = 0; i < _attributes.Length; i++)
                {
                    object attribute = _attributes[i];
                    if (attributeType.IsInstanceOfType(attribute))
                    {
                        matches.Add(attribute);
                    }
                }

                return matches.ToArray();
            }

            public override object[] GetCustomAttributes(bool inherit)
            {
                return (object[])_attributes.Clone();
            }

            public override bool IsDefined(Type attributeType, bool inherit)
            {
                return GetCustomAttributes(attributeType, inherit).Length > 0;
            }

            public override MemberTypes MemberType => MemberTypes.Method;

            public override string Name => "FakeMember";

            public override Type DeclaringType => typeof(FakeMemberInfo);

            public override Type ReflectedType => typeof(FakeMemberInfo);

            public override Module Module => typeof(FakeMemberInfo).Module;
        }

        private interface ISampleInterface { }

        private class BaseSample : ISampleInterface { }

        private sealed class DerivedSample : BaseSample { }

        private sealed class ThrowingAssembly : Assembly
        {
            public override string FullName => "NovaSharp.Tests.ThrowingAssembly";

            public override string Location => string.Empty;

            public override IEnumerable<CustomAttributeData> CustomAttributes =>
                Array.Empty<CustomAttributeData>();

            public override bool ReflectionOnly => false;

            [Obsolete]
            public override bool GlobalAssemblyCache => false;

            public override AssemblyName GetName(bool copiedName)
            {
                return new AssemblyName("NovaSharp.Tests.ThrowingAssembly");
            }

            public override Type[] GetTypes()
            {
                throw new ReflectionTypeLoadException(
                    Array.Empty<Type>(),
                    Array.Empty<Exception>()
                );
            }

            public override string[] GetManifestResourceNames()
            {
                return Array.Empty<string>();
            }

            public override Module[] GetModules(bool getResourceModules)
            {
                return Array.Empty<Module>();
            }

            public override Module GetModule(string name)
            {
                throw new NotImplementedException();
            }

            public override AssemblyName GetName()
            {
                return GetName(false);
            }

            public override FileStream GetFile(string name)
            {
                throw new NotImplementedException();
            }

            public override FileStream[] GetFiles(bool getResourceModules)
            {
                return Array.Empty<FileStream>();
            }

            public override Stream GetManifestResourceStream(string name)
            {
                return null;
            }

            public override Stream GetManifestResourceStream(Type type, string name)
            {
                return null;
            }

            public override object[] GetCustomAttributes(Type attributeType, bool inherit)
            {
                return Array.Empty<object>();
            }

            public override object[] GetCustomAttributes(bool inherit)
            {
                return Array.Empty<object>();
            }

            public override bool IsDefined(Type attributeType, bool inherit)
            {
                return false;
            }

            public override ManifestResourceInfo GetManifestResourceInfo(string resourceName)
            {
                return null;
            }
        }
    }

    public sealed class DescriptorHelpersPublicType { }

    internal sealed class DescriptorHelpersTestsInternalTopLevel
    {
        internal static readonly DescriptorHelpersTestsInternalTopLevel Instance = new();
    }
}
