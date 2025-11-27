namespace NovaSharp.Interpreter.Tests.Units
{
    using System;
    using System.Collections.Generic;
    using System.Diagnostics.CodeAnalysis;
    using System.IO;
    using System.Linq;
    using System.Linq.Expressions;
    using System.Reflection;
    using System.Reflection.Emit;
    using NovaSharp.Interpreter.Interop;
    using NovaSharp.Interpreter.Interop.Attributes;
    using NUnit.Framework;

    [TestFixture]
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
            _ = new MemberVisibilityFixtures();
            _ = new PropertyFixtures();
            _ = new MetaFixtures();
            _ = new DerivedSample();
        }

        [Test]
        public void GetVisibilityFromAttributesHandlesNullAndExplicitAttributes()
        {
            Assert.That(DescriptorHelpers.GetVisibilityFromAttributes(null), Is.False);

            MemberInfo visible = VisibilityTargets.Metadata.VisibleMember;
            MemberInfo hidden = VisibilityTargets.Metadata.HiddenMember;
            MemberInfo overridden = VisibilityTargets.Metadata.ForcedHiddenMember;

            Assert.That(visible.GetVisibilityFromAttributes(), Is.True);
            Assert.That(hidden.GetVisibilityFromAttributes(), Is.False);
            Assert.That(overridden.GetVisibilityFromAttributes(), Is.False);
        }

        [Test]
        public void GetVisibilityFromAttributesThrowsWhenVisibleAttributeDuplicated()
        {
            MemberInfo member = new FakeMemberInfo(
                new NovaSharpVisibleAttribute(true),
                new NovaSharpVisibleAttribute(true)
            );

            Assert.That(
                () => member.GetVisibilityFromAttributes(),
                Throws.InvalidOperationException.With.Message.Contains("VisibleAttribute")
            );
        }

        [Test]
        public void GetVisibilityFromAttributesThrowsWhenHiddenAttributeDuplicated()
        {
            MemberInfo member = new FakeMemberInfo(
                new NovaSharpHiddenAttribute(),
                new NovaSharpHiddenAttribute()
            );

            Assert.That(
                () => member.GetVisibilityFromAttributes(),
                Throws.InvalidOperationException.With.Message.Contains("HiddenAttribute")
            );
        }

        [Test]
        public void GetVisibilityFromAttributesThrowsWhenAttributesConflict()
        {
            MemberInfo conflicting = VisibilityTargets.Metadata.ConflictingMember;

            Assert.That(
                () => conflicting.GetVisibilityFromAttributes(),
                Throws.InvalidOperationException.With.Message.Contains("discording")
            );
        }

        [Test]
        public void DescriptorVisibilityHelpersValidateNullArguments()
        {
            FieldInfo field = MemberVisibilityFixtures.Metadata.PublicField;
            PropertyInfo property = PropertyFixtures.Metadata.GetterOnly;
            MethodInfo method = MemberVisibilityFixtures.Metadata.PublicMethod;

            Assert.Multiple(() =>
            {
                Assert.That(
                    () => DescriptorHelpers.GetClrVisibility((Type)null),
                    Throws.ArgumentNullException.With.Property("ParamName").EqualTo("type")
                );
                Assert.That(
                    () => DescriptorHelpers.GetClrVisibility((FieldInfo)null),
                    Throws.ArgumentNullException.With.Property("ParamName").EqualTo("info")
                );
                Assert.That(
                    () => DescriptorHelpers.GetClrVisibility((PropertyInfo)null),
                    Throws.ArgumentNullException.With.Property("ParamName").EqualTo("info")
                );
                Assert.That(
                    () => DescriptorHelpers.GetClrVisibility((MethodBase)null),
                    Throws.ArgumentNullException.With.Property("ParamName").EqualTo("info")
                );
                Assert.That(
                    () => DescriptorHelpers.IsPropertyInfoPublic(null),
                    Throws.ArgumentNullException.With.Property("ParamName").EqualTo("pi")
                );
                Assert.That(
                    () => DescriptorHelpers.GetMetaNamesFromAttributes(null),
                    Throws.ArgumentNullException.With.Property("ParamName").EqualTo("mi")
                );
                Assert.That(
                    () => DescriptorHelpers.GetConversionMethodName(null),
                    Throws.ArgumentNullException.With.Property("ParamName").EqualTo("type")
                );
            });

            // Sanity-check that the original helpers still work with valid inputs.
            Assert.Multiple(() =>
            {
                Assert.That(field.GetClrVisibility(), Is.EqualTo("public"));
                Assert.That(property.IsPropertyInfoPublic(), Is.True);
                Assert.That(method.GetClrVisibility(), Is.EqualTo("public"));
            });
        }

        [Test]
        public void GetClrVisibilityReturnsExpectedValuesForTypes()
        {
            Type protectedInternal = VisibilityFixtures.Metadata.ProtectedInternalType;
            Type protectedNested = VisibilityFixtures.Metadata.ProtectedType;
            Type privateNested = VisibilityFixtures.Metadata.PrivateType;

            Assert.Multiple(() =>
            {
                Assert.That(
                    typeof(DescriptorHelpersPublicType).GetClrVisibility(),
                    Is.EqualTo("public")
                );
                Assert.That(
                    typeof(DescriptorHelpersTestsInternalTopLevel).GetClrVisibility(),
                    Is.EqualTo("internal")
                );
                Assert.That(
                    protectedInternal!.GetClrVisibility(),
                    Is.EqualTo("protected-internal")
                );
                Assert.That(protectedNested!.GetClrVisibility(), Is.EqualTo("protected"));
                Assert.That(privateNested!.GetClrVisibility(), Is.EqualTo("private"));
            });
        }

        [Test]
        public void GetClrVisibilityReturnsExpectedValuesForMembers()
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

            Assert.Multiple(() =>
            {
                Assert.That(publicField.GetClrVisibility(), Is.EqualTo("public"));
                Assert.That(internalField.GetClrVisibility(), Is.EqualTo("internal"));
                Assert.That(protectedField.GetClrVisibility(), Is.EqualTo("protected"));
                Assert.That(
                    protectedInternalField.GetClrVisibility(),
                    Is.EqualTo("protected-internal")
                );
                Assert.That(privateMethod.GetClrVisibility(), Is.EqualTo("private"));
                Assert.That(publicMethod.GetClrVisibility(), Is.EqualTo("public"));
                Assert.That(
                    protectedInternalMethod.GetClrVisibility(),
                    Is.EqualTo("protected-internal")
                );
            });
        }

        [Test]
        public void GetClrVisibilityReturnsExpectedValuesForProperties()
        {
            PropertyInfo publicProperty = PropertyFixtures.Metadata.GetterOnly;
            PropertyInfo internalProperty = PropertyFixtures.Metadata.InternalAccessors;
            PropertyInfo protectedProperty = PropertyFixtures.Metadata.ProtectedGetterOnly;

            Assert.Multiple(() =>
            {
                Assert.That(publicProperty.GetClrVisibility(), Is.EqualTo("public"));
                Assert.That(internalProperty.GetClrVisibility(), Is.EqualTo("internal"));
                Assert.That(protectedProperty.GetClrVisibility(), Is.EqualTo("protected"));
            });
        }

        [Test]
        public void IsPropertyInfoPublicConsidersGetterOrSetter()
        {
            PropertyInfo getterOnly = PropertyFixtures.Metadata.GetterOnly;
            PropertyInfo setterOnly = PropertyFixtures.Metadata.SetterOnly;
            PropertyInfo privateProperty = PropertyFixtures.Metadata.PrivateBoth;

            Assert.Multiple(() =>
            {
                Assert.That(getterOnly.IsPropertyInfoPublic(), Is.True);
                Assert.That(setterOnly.IsPropertyInfoPublic(), Is.True);
                Assert.That(privateProperty.IsPropertyInfoPublic(), Is.False);
            });
        }

        [Test]
        public void GetMetaNamesFromAttributesReturnsDeclaredNames()
        {
            MethodInfo method = MetaFixtures.Metadata.Metamethods;

            IReadOnlyList<string> names = method.GetMetaNamesFromAttributes();

            Assert.That(names, Is.EquivalentTo(ExpectedMetaNames));
        }

        [Test]
        public void GetMetaNamesFromAttributesReturnsEmptyWhenNoAttributesPresent()
        {
            MethodInfo method = MetaFixtures.Metadata.PlainMethod;

            IReadOnlyList<string> names = method.GetMetaNamesFromAttributes();

            Assert.That(names, Is.Empty);
        }

        [Test]
        public void GetConversionMethodNameSanitizesNonAlphaNumericCharacters()
        {
            string name =
                typeof(VisibilityFixtures.GenericHost<string>.InnerType).GetConversionMethodName();

            Assert.That(name, Is.EqualTo("__toInnerType"));

            string genericName = typeof(VisibilityFixtures.GenericHost<>).GetConversionMethodName();
            Assert.That(genericName, Is.EqualTo("__toGenericHost_1"));
        }

        [Test]
        public void IsDelegateTypeIdentifiesDelegates()
        {
            Assert.Multiple(() =>
            {
                Assert.That(typeof(Action).IsDelegateType(), Is.True);
                Assert.That(typeof(string).IsDelegateType(), Is.False);
            });
        }

        [Test]
        public void GetAllImplementedTypesIncludesBaseTypesAndInterfaces()
        {
            IEnumerable<Type> types = typeof(DerivedSample).GetAllImplementedTypes();

            Assert.Multiple(() =>
            {
                Assert.That(types, Does.Contain(typeof(DerivedSample)));
                Assert.That(types, Does.Contain(typeof(BaseSample)));
                Assert.That(types, Does.Contain(typeof(ISampleInterface)));
            });
        }

        [Test]
        public void SafeGetTypesReturnsEmptyArrayWhenAssemblyFailsToLoadTypes()
        {
            Assembly throwingAssembly = new ThrowingAssembly();

            Type[] types = throwingAssembly.SafeGetTypes();

            Assert.That(types, Is.Empty);
        }

        [Test]
        public void IdentifierHelpersValidateAndNormalizeNames()
        {
            Assert.Multiple(() =>
            {
                Assert.That(DescriptorHelpers.IsValidSimpleIdentifier("value_1"), Is.True);
                Assert.That(DescriptorHelpers.IsValidSimpleIdentifier("1value"), Is.False);
                Assert.That(DescriptorHelpers.IsValidSimpleIdentifier("value-1"), Is.False);
                Assert.That(
                    DescriptorHelpers.ToValidSimpleIdentifier("1 invalid-name"),
                    Is.EqualTo("_1_invalid_name")
                );
                Assert.That(DescriptorHelpers.IsValidSimpleIdentifier(null), Is.False);
                Assert.That(DescriptorHelpers.ToValidSimpleIdentifier(null), Is.EqualTo("_"));
                Assert.That(
                    DescriptorHelpers.ToValidSimpleIdentifier(string.Empty),
                    Is.EqualTo("_")
                );
            });
        }

        [Test]
        public void NameTransformationsCoverCamelCaseAndSnakeCaseVariants()
        {
            Assert.Multiple(() =>
            {
                Assert.That(
                    DescriptorHelpers.Camelify("my_sample_value"),
                    Is.EqualTo("mySampleValue")
                );
                Assert.That(
                    DescriptorHelpers.Camelify("__Already__Mixed__"),
                    Is.EqualTo("alreadyMixed")
                );
                Assert.That(DescriptorHelpers.Camelify("___"), Is.EqualTo(string.Empty));
                Assert.That(
                    DescriptorHelpers.ToUpperUnderscore("HttpRequestV2"),
                    Is.EqualTo("HTTP_REQUEST_V2")
                );
                Assert.That(
                    DescriptorHelpers.ToUpperUnderscore("Version42Beta"),
                    Is.EqualTo("VERSION_42_BETA")
                );
                Assert.That(
                    DescriptorHelpers.ToUpperUnderscore("value-with-dash"),
                    Is.EqualTo("VALUE_WITH_DASH")
                );
                Assert.That(
                    DescriptorHelpers.ToUpperUnderscore("Value123Name"),
                    Is.EqualTo("VALUE_123_NAME")
                );
                Assert.That(
                    DescriptorHelpers.ToUpperUnderscore("Value123"),
                    Is.EqualTo("VALUE123")
                );
                Assert.That(DescriptorHelpers.UpperFirstLetter("sample"), Is.EqualTo("Sample"));
                Assert.That(
                    DescriptorHelpers.NormalizeUppercaseRuns("HTTPRequestURL"),
                    Is.EqualTo("HttpRequestUrl")
                );
                Assert.That(
                    DescriptorHelpers.NormalizeUppercaseRuns("GPUFPSCounter"),
                    Is.EqualTo("GpufpsCounter"),
                    "Acronym chains remain collapsed; revisit when descriptor naming adopts full tokenization."
                );
                Assert.That(
                    DescriptorHelpers.NormalizeUppercaseRuns(string.Empty),
                    Is.EqualTo(string.Empty)
                );
                Assert.That(
                    DescriptorHelpers.NormalizeUppercaseRuns("___"),
                    Is.EqualTo(string.Empty)
                );
                Assert.That(DescriptorHelpers.NormalizeUppercaseRuns(null), Is.Null);
                Assert.That(DescriptorHelpers.ToUpperUnderscore(null), Is.Null);
                Assert.That(
                    () => DescriptorHelpers.Camelify(null),
                    Throws.ArgumentNullException.With.Property("ParamName").EqualTo("name")
                );
            });
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

            protected internal sealed class ProtectedInternalNested { }

            protected sealed class ProtectedNested { }

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

        [SuppressMessage(
            "Performance",
            "CA1852:Seal internal types",
            Justification = "Protected members must remain available for visibility reflection tests without triggering CS0628 warnings."
        )]
        private class MemberVisibilityFixtures
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

        [SuppressMessage(
            "Performance",
            "CA1852:Seal internal types",
            Justification = "Provides protected members for visibility tests; sealing would trigger compiler warnings."
        )]
        private class PropertyFixtures
        {
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
