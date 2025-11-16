namespace NovaSharp.Interpreter.Tests.Units
{
    using System;
    using System.Collections.Generic;
    using System.IO;
    using System.Linq;
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

            MemberInfo visible = typeof(VisibilityTargets).GetMethod(
                nameof(VisibilityTargets.VisibleMember),
                BindingFlags.Instance | BindingFlags.Public
            );
            MemberInfo hidden = typeof(VisibilityTargets).GetMethod(
                nameof(VisibilityTargets.HiddenMember),
                BindingFlags.Instance | BindingFlags.Public
            );
            MemberInfo overridden = typeof(VisibilityTargets).GetMethod(
                nameof(VisibilityTargets.ForcedHidden),
                BindingFlags.Instance | BindingFlags.Public
            );

            Assert.That(visible.GetVisibilityFromAttributes(), Is.True);
            Assert.That(hidden.GetVisibilityFromAttributes(), Is.False);
            Assert.That(overridden.GetVisibilityFromAttributes(), Is.False);
        }

        [Test]
        public void GetVisibilityFromAttributesThrowsWhenAttributesConflict()
        {
            MemberInfo conflicting = typeof(VisibilityTargets).GetMethod(
                nameof(VisibilityTargets.ConflictingMember),
                BindingFlags.Instance | BindingFlags.Public
            );

            Assert.That(
                () => conflicting.GetVisibilityFromAttributes(),
                Throws.InvalidOperationException.With.Message.Contains("discording")
            );
        }

        [Test]
        public void GetClrVisibilityReturnsExpectedValuesForTypes()
        {
            Type protectedInternal = typeof(VisibilityFixtures).GetNestedType(
                "ProtectedInternalNested",
                BindingFlags.NonPublic | BindingFlags.Public
            );
            Type protectedNested = typeof(VisibilityFixtures).GetNestedType(
                "ProtectedNested",
                BindingFlags.NonPublic | BindingFlags.Public
            );
            Type privateNested = typeof(VisibilityFixtures).GetNestedType(
                "PrivateNested",
                BindingFlags.NonPublic | BindingFlags.Public
            );

            Assert.Multiple(() =>
            {
                Assert.That(typeof(PublicType).GetClrVisibility(), Is.EqualTo("public"));
                Assert.That(
                    typeof(DescriptorHelpersTests_InternalTopLevel).GetClrVisibility(),
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
            FieldInfo publicField = typeof(MemberVisibilityFixtures).GetField(
                nameof(MemberVisibilityFixtures.PublicField),
                BindingFlags.Instance | BindingFlags.Public
            );
            FieldInfo internalField = typeof(MemberVisibilityFixtures).GetField(
                MemberVisibilityFixtures.InternalFieldName,
                BindingFlags.Instance | BindingFlags.NonPublic | BindingFlags.Public
            );
            FieldInfo protectedField = typeof(MemberVisibilityFixtures).GetField(
                MemberVisibilityFixtures.ProtectedFieldName,
                BindingFlags.Instance | BindingFlags.NonPublic | BindingFlags.Public
            );
            FieldInfo protectedInternalField = typeof(MemberVisibilityFixtures).GetField(
                nameof(MemberVisibilityFixtures.ProtectedInternalField),
                BindingFlags.Instance | BindingFlags.NonPublic | BindingFlags.Public
            );
            MethodBase privateMethod = typeof(MemberVisibilityFixtures).GetMethod(
                MemberVisibilityFixtures.PrivateMethodName,
                BindingFlags.Instance | BindingFlags.NonPublic | BindingFlags.Public
            );
            MethodBase publicMethod = typeof(MemberVisibilityFixtures).GetMethod(
                nameof(MemberVisibilityFixtures.PublicMethod),
                BindingFlags.Instance | BindingFlags.Public
            );
            MethodBase protectedInternalMethod = typeof(MemberVisibilityFixtures).GetMethod(
                nameof(MemberVisibilityFixtures.ProtectedInternalMethod),
                BindingFlags.Instance | BindingFlags.NonPublic | BindingFlags.Public
            );

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
            PropertyInfo publicProperty = typeof(PropertyFixtures).GetProperty(
                nameof(PropertyFixtures.GetterOnly),
                BindingFlags.Instance | BindingFlags.Public
            );
            PropertyInfo internalProperty = typeof(PropertyFixtures).GetProperty(
                nameof(PropertyFixtures.InternalAccessors),
                BindingFlags.Instance | BindingFlags.NonPublic | BindingFlags.Public
            );
            PropertyInfo protectedProperty = typeof(PropertyFixtures).GetProperty(
                "ProtectedGetterOnly",
                BindingFlags.Instance | BindingFlags.NonPublic | BindingFlags.Public
            );

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
            PropertyInfo getterOnly = typeof(PropertyFixtures).GetProperty(
                nameof(PropertyFixtures.GetterOnly),
                BindingFlags.Instance | BindingFlags.Public
            );
            PropertyInfo setterOnly = typeof(PropertyFixtures).GetProperty(
                nameof(PropertyFixtures.SetterOnly),
                BindingFlags.Instance | BindingFlags.Public
            );
            PropertyInfo privateProperty = typeof(PropertyFixtures).GetProperty(
                PropertyFixtures.PrivatePropertyName,
                BindingFlags.Instance | BindingFlags.NonPublic | BindingFlags.Public
            );

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
            MethodInfo method = typeof(MetaFixtures).GetMethod(
                nameof(MetaFixtures.Metamethods),
                BindingFlags.Instance | BindingFlags.Public
            );

            List<string> names = method.GetMetaNamesFromAttributes();

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
        }

        public class PublicType { }

        internal sealed class DescriptorHelpersTests_InternalTopLevel { }

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
        }

        public sealed class PropertyFixtures
        {
            public const string PrivatePropertyName = nameof(PrivateBoth);

            public int GetterOnly { get; private set; }

            public int SetterOnly { private get; set; }

            internal int InternalAccessors { get; set; }

            protected int ProtectedGetterOnly { get; private set; }

            private int PrivateBoth { get; set; }
        }

        private sealed class MetaFixtures
        {
            [NovaSharpUserDataMetamethod("__index")]
            [NovaSharpUserDataMetamethod("__len")]
            public void Metamethods() { }
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
