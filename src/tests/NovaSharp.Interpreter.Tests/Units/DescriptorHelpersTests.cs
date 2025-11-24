namespace NovaSharp.Interpreter.Tests.Units
{
    using System;
    using System.Collections.Generic;
    using System.IO;
    using System.Linq;
    using System.Linq.Expressions;
    using System.Reflection;
    using NovaSharp.Interpreter.Interop;
    using NovaSharp.Interpreter.Interop.Attributes;
    using NUnit.Framework;

    [TestFixture]
    public sealed class DescriptorHelpersTests
    {
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
        public void GetVisibilityFromAttributesThrowsWhenAttributesConflict()
        {
            MemberInfo conflicting = VisibilityTargets.Metadata.ConflictingMember;

            Assert.That(
                () => conflicting.GetVisibilityFromAttributes(),
                Throws.InvalidOperationException.With.Message.Contains("discording")
            );
        }

        [Test]
        public void GetClrVisibilityReturnsExpectedValuesForTypes()
        {
            Type protectedInternal = VisibilityFixtures.Metadata.ProtectedInternalType;
            Type protectedNested = VisibilityFixtures.Metadata.ProtectedType;
            Type privateNested = VisibilityFixtures.Metadata.PrivateType;

            Assert.Multiple(() =>
            {
                Assert.That(typeof(PublicType).GetClrVisibility(), Is.EqualTo("public"));
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
            MethodBase privateMethod = MemberVisibilityFixtures.Metadata.PrivateMethod;
            MethodBase publicMethod = MemberVisibilityFixtures.Metadata.PublicMethod;
            MethodBase protectedInternalMethod = MemberVisibilityFixtures
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

            Assert.That(names, Is.EquivalentTo(new[] { "__index", "__len" }));
        }

        [Test]
        public void GetConversionMethodNameSanitizesNonAlphaNumericCharacters()
        {
            string name =
                typeof(VisibilityFixtures.GenericHost<string>.InnerType).GetConversionMethodName();

            Assert.That(name, Is.EqualTo("__toInnerType"));
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
                Assert.That(
                    DescriptorHelpers.ToValidSimpleIdentifier("1 invalid-name"),
                    Is.EqualTo("_1_invalid_name")
                );
                Assert.That(DescriptorHelpers.IsValidSimpleIdentifier(null), Is.False);
                Assert.That(DescriptorHelpers.ToValidSimpleIdentifier(null), Is.EqualTo("_"));
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
            });
        }

        private sealed class VisibilityTargets
        {
            [NovaSharpVisible(true)]
            public void VisibleMember() { }

            [NovaSharpHidden]
            public void HiddenMember() { }

            [NovaSharpVisible(false)]
            public void ForcedHidden() { }

            [NovaSharpVisible(true)]
            [NovaSharpHidden]
            public void ConflictingMember() { }

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

        public class PublicType { }

        internal sealed class DescriptorHelpersTestsInternalTopLevel { }

        public class VisibilityFixtures
        {
            public class PublicNested { }

            protected internal class ProtectedInternalNested { }

            protected class ProtectedNested { }

            private sealed class PrivateNested { }

            public class GenericHost<T>
            {
                public class InnerType { }
            }

            internal static class Metadata
            {
                internal static Type ProtectedInternalType => typeof(ProtectedInternalNested);

                internal static Type ProtectedType => typeof(ProtectedNested);

                internal static Type PrivateType => typeof(PrivateNested);
            }
        }

        public sealed class MemberVisibilityFixtures
        {
            public const string InternalFieldName = nameof(InternalField);
            public const string ProtectedFieldName = nameof(ProtectedField);
            public const string PrivateMethodName = nameof(PrivateMethod);

            public int PublicField = 0;

            internal int InternalField = 0;

            protected int ProtectedField = 0;

            protected internal int ProtectedInternalField = 0;

            public void PublicMethod() { }

            private void PrivateMethod() { }

            protected internal void ProtectedInternalMethod() { }

            internal static class Metadata
            {
                internal static FieldInfo PublicField { get; } =
                    typeof(MemberVisibilityFixtures).GetField(nameof(PublicField))!;

                internal static FieldInfo InternalField { get; } =
                    GetInstanceField(f => f.InternalField);

                internal static FieldInfo ProtectedField { get; } =
                    GetInstanceField(f => f.ProtectedField);

                internal static FieldInfo ProtectedInternalField { get; } =
                    GetInstanceField(f => f.ProtectedInternalField);

                internal static MethodBase PrivateMethod { get; } =
                    GetInstanceMethod(f => f.PrivateMethod());

                internal static MethodBase PublicMethod { get; } =
                    typeof(MemberVisibilityFixtures).GetMethod(nameof(PublicMethod))!;

                internal static MethodBase ProtectedInternalMethod { get; } =
                    GetInstanceMethod(f => f.ProtectedInternalMethod());

                private static FieldInfo GetInstanceField<TValue>(
                    Expression<Func<MemberVisibilityFixtures, TValue>> accessor
                )
                {
                    return (FieldInfo)GetMemberExpression(accessor.Body).Member;
                }

                private static MethodBase GetInstanceMethod(
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

        public sealed class PropertyFixtures
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
            [NovaSharpUserDataMetamethod("__index")]
            [NovaSharpUserDataMetamethod("__len")]
            public void Metamethods() { }

            internal static class Metadata
            {
                internal static MethodInfo Metamethods { get; } =
                    typeof(MetaFixtures).GetMethod(nameof(Metamethods))!;
            }
        }

        public interface ISampleInterface { }

        public class BaseSample : ISampleInterface { }

        public sealed class DerivedSample : BaseSample { }

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
}
