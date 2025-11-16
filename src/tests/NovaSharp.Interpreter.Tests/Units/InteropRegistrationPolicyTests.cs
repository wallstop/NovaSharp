namespace NovaSharp.Interpreter.Tests.Units
{
    using System;
    using System.Linq;
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
            IRegistrationPolicy policy = InteropRegistrationPolicy.Explicit;

            Assert.Multiple(() =>
            {
                Assert.That(policy, Is.InstanceOf<DefaultRegistrationPolicy>());

#pragma warning disable CS0618
                System.Reflection.PropertyInfo method =
                    typeof(InteropRegistrationPolicy).GetProperty(
                        nameof(InteropRegistrationPolicy.Explicit)
                    );
#pragma warning restore CS0618
                object[] attributes =
                    method != null
                        ? method.GetCustomAttributes(typeof(ObsoleteAttribute), false)
                        : Array.Empty<object>();
                bool hasObsolete = attributes.OfType<ObsoleteAttribute>().Any();

                Assert.That(hasObsolete, Is.True);
            });
        }
    }
}
