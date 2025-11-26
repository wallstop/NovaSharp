namespace NovaSharp.Interpreter.Tests.Units
{
    using System;
    using System.Linq;
    using System.Reflection;
    using NovaSharp.Interpreter.Interop;
    using NovaSharp.Interpreter.Interop.RegistrationPolicies;
    using NUnit.Framework;

    [TestFixture]
    public sealed class InteropRegistrationPolicyTests
    {
        [Test]
        public void DefaultReturnsNewDefaultPolicyInstance()
        {
            IRegistrationPolicy first = InteropRegistrationPolicy.Default;
            IRegistrationPolicy second = InteropRegistrationPolicy.Default;

            Assert.Multiple(() =>
            {
                Assert.That(first, Is.InstanceOf<DefaultRegistrationPolicy>());
                Assert.That(second, Is.InstanceOf<DefaultRegistrationPolicy>());
                Assert.That(first, Is.Not.SameAs(second));
            });
        }

        [Test]
        public void AutomaticReturnsAutomaticPolicy()
        {
            IRegistrationPolicy policy = InteropRegistrationPolicy.Automatic;
            Assert.That(policy, Is.InstanceOf<AutomaticRegistrationPolicy>());
        }

        [Test]
        public void ExplicitReturnsDefaultPolicyAndIsMarkedObsolete()
        {
            PropertyInfo property = typeof(InteropRegistrationPolicy).GetProperty("Explicit");
            Assert.That(property, Is.Not.Null);

            IRegistrationPolicy policy = GetExplicitPolicy(property);

            Assert.Multiple(() =>
            {
                Assert.That(policy, Is.InstanceOf<DefaultRegistrationPolicy>());
                Assert.That(IsExplicitPropertyObsolete(property), Is.True);
            });
        }

        private static IRegistrationPolicy GetExplicitPolicy(PropertyInfo property)
        {
#pragma warning disable CS0618
            object value = property.GetValue(null);
#pragma warning restore CS0618
            return (IRegistrationPolicy)value;
        }

        private static bool IsExplicitPropertyObsolete(PropertyInfo property)
        {
            object[] attributes = property.GetCustomAttributes(
                typeof(ObsoleteAttribute),
                inherit: false
            );
            return attributes.OfType<ObsoleteAttribute>().Any();
        }
    }
}
