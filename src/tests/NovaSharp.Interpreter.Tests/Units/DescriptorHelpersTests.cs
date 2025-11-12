namespace NovaSharp.Interpreter.Tests.Units
{
    using System;
    using System.Collections.Generic;
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

            MemberInfo visible =
                typeof(VisibilityTargets)
                    .GetMethod(nameof(VisibilityTargets.VisibleMember), BindingFlags.Instance | BindingFlags.Public);
            MemberInfo hidden =
                typeof(VisibilityTargets)
                    .GetMethod(nameof(VisibilityTargets.HiddenMember), BindingFlags.Instance | BindingFlags.Public);
            MemberInfo overridden =
                typeof(VisibilityTargets)
                    .GetMethod(nameof(VisibilityTargets.ForcedHidden), BindingFlags.Instance | BindingFlags.Public);

            Assert.That(visible.GetVisibilityFromAttributes(), Is.True);
            Assert.That(hidden.GetVisibilityFromAttributes(), Is.False);
            Assert.That(overridden.GetVisibilityFromAttributes(), Is.False);
        }

        [Test]
        public void GetVisibilityFromAttributesThrowsWhenAttributesConflict()
        {
            MemberInfo conflicting =
                typeof(VisibilityTargets)
                    .GetMethod(nameof(VisibilityTargets.ConflictingMember), BindingFlags.Instance | BindingFlags.Public);

            Assert.That(
                () => conflicting.GetVisibilityFromAttributes(),
                Throws.InvalidOperationException.With.Message.Contains("discording")
            );
        }

        [Test]
        public void GetClrVisibilityReturnsExpectedValuesForTypes()
        {
            Type protectedInternal =
                typeof(VisibilityFixtures).GetNestedType(
                    "ProtectedInternalNested",
                    BindingFlags.NonPublic | BindingFlags.Public
                );
            Type protectedNested =
                typeof(VisibilityFixtures).GetNestedType(
                    "ProtectedNested",
                    BindingFlags.NonPublic | BindingFlags.Public
                );
            Type privateNested =
                typeof(VisibilityFixtures).GetNestedType(
                    "PrivateNested",
                    BindingFlags.NonPublic | BindingFlags.Public
                );

            Assert.Multiple(() =>
            {
                Assert.That(typeof(PublicType).GetClrVisibility(), Is.EqualTo("public"));
                Assert.That(typeof(DescriptorHelpersTests_InternalTopLevel).GetClrVisibility(), Is.EqualTo("internal"));
                Assert.That(protectedInternal!.GetClrVisibility(), Is.EqualTo("protected-internal"));
                Assert.That(protectedNested!.GetClrVisibility(), Is.EqualTo("protected"));
                Assert.That(privateNested!.GetClrVisibility(), Is.EqualTo("private"));
            });
        }

        [Test]
        public void GetClrVisibilityReturnsExpectedValuesForMembers()
        {
            FieldInfo internalField =
                typeof(MemberVisibilityFixtures)
                    .GetField(MemberVisibilityFixtures.InternalFieldName, BindingFlags.Instance | BindingFlags.NonPublic | BindingFlags.Public);
            FieldInfo protectedField =
                typeof(MemberVisibilityFixtures)
                    .GetField(MemberVisibilityFixtures.ProtectedFieldName, BindingFlags.Instance | BindingFlags.NonPublic | BindingFlags.Public);
            MethodBase privateMethod =
                typeof(MemberVisibilityFixtures)
                    .GetMethod(MemberVisibilityFixtures.PrivateMethodName, BindingFlags.Instance | BindingFlags.NonPublic | BindingFlags.Public);
            MethodBase publicMethod =
                typeof(MemberVisibilityFixtures)
                    .GetMethod(nameof(MemberVisibilityFixtures.PublicMethod), BindingFlags.Instance | BindingFlags.Public);

            Assert.Multiple(() =>
            {
                Assert.That(internalField.GetClrVisibility(), Is.EqualTo("internal"));
                Assert.That(protectedField.GetClrVisibility(), Is.EqualTo("protected"));
                Assert.That(privateMethod.GetClrVisibility(), Is.EqualTo("private"));
                Assert.That(publicMethod.GetClrVisibility(), Is.EqualTo("public"));
            });
        }

        [Test]
        public void IsPropertyInfoPublicConsidersGetterOrSetter()
        {
            PropertyInfo getterOnly =
                typeof(PropertyFixtures)
                    .GetProperty(nameof(PropertyFixtures.GetterOnly), BindingFlags.Instance | BindingFlags.Public);
            PropertyInfo setterOnly =
                typeof(PropertyFixtures)
                    .GetProperty(nameof(PropertyFixtures.SetterOnly), BindingFlags.Instance | BindingFlags.Public);
            PropertyInfo privateProperty =
                typeof(PropertyFixtures)
                    .GetProperty(PropertyFixtures.PrivatePropertyName, BindingFlags.Instance | BindingFlags.NonPublic | BindingFlags.Public);

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
            MethodInfo method =
                typeof(MetaFixtures)
                    .GetMethod(nameof(MetaFixtures.Metamethods), BindingFlags.Instance | BindingFlags.Public);

            List<string> names = method.GetMetaNamesFromAttributes();

            Assert.That(names, Is.EquivalentTo(new[] { "__index", "__len" }));
        }

        [Test]
        public void GetConversionMethodNameSanitizesNonAlphaNumericCharacters()
        {
            string name = typeof(VisibilityFixtures.GenericHost<string>.InnerType).GetConversionMethodName();

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
        public void IdentifierHelpersValidateAndNormalizeNames()
        {
            Assert.Multiple(() =>
            {
                Assert.That(DescriptorHelpers.IsValidSimpleIdentifier("value_1"), Is.True);
                Assert.That(DescriptorHelpers.IsValidSimpleIdentifier("1value"), Is.False);
                Assert.That(DescriptorHelpers.ToValidSimpleIdentifier("1 invalid-name"), Is.EqualTo("_1_invalid_name"));
            });
        }

        [Test]
        public void NameTransformationsCoverCamelCaseAndSnakeCaseVariants()
        {
            Assert.Multiple(() =>
            {
                Assert.That(DescriptorHelpers.Camelify("my_sample_value"), Is.EqualTo("mySampleValue"));
                Assert.That(DescriptorHelpers.ToUpperUnderscore("HttpRequestV2"), Is.EqualTo("HTTP_REQUEST_V2"));
                Assert.That(DescriptorHelpers.ToUpperUnderscore("Version42Beta"), Is.EqualTo("VERSION_42_BETA"));
                Assert.That(DescriptorHelpers.UpperFirstLetter("sample"), Is.EqualTo("Sample"));
                Assert.That(DescriptorHelpers.NormalizeUppercaseRuns("HTTPRequestURL"), Is.EqualTo("HttpRequestUrl"));
                Assert.That(
                    DescriptorHelpers.NormalizeUppercaseRuns("GPUFPSCounter"),
                    Is.EqualTo("GpufpsCounter"),
                    "Acronym chains remain collapsed; revisit when descriptor naming adopts full tokenization."
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

            internal int InternalField = 0;

            protected int ProtectedField = 0;

            public void PublicMethod() { }

            private void PrivateMethod() { }
        }

        public sealed class PropertyFixtures
        {
            public const string PrivatePropertyName = nameof(PrivateBoth);

            public int GetterOnly { get; private set; }

            public int SetterOnly { private get; set; }

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
    }
}
