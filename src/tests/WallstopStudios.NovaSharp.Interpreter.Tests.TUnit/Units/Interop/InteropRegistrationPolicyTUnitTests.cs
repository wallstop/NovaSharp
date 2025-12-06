namespace WallstopStudios.NovaSharp.Interpreter.Tests.TUnit.Units.Interop
{
    using System;
    using System.Linq;
    using System.Reflection;
    using System.Threading.Tasks;
    using global::TUnit.Assertions;
    using WallstopStudios.NovaSharp.Interpreter.Interop;
    using WallstopStudios.NovaSharp.Interpreter.Interop.RegistrationPolicies;

    public sealed class InteropRegistrationPolicyTUnitTests
    {
        [global::TUnit.Core.Test]
        public async Task DefaultReturnsNewDefaultPolicyInstance()
        {
            IRegistrationPolicy first = InteropRegistrationPolicy.Default;
            IRegistrationPolicy second = InteropRegistrationPolicy.Default;

            await Assert.That(first).IsTypeOf<DefaultRegistrationPolicy>().ConfigureAwait(false);
            await Assert.That(second).IsTypeOf<DefaultRegistrationPolicy>().ConfigureAwait(false);
            await Assert.That(ReferenceEquals(first, second)).IsFalse().ConfigureAwait(false);
        }

        [global::TUnit.Core.Test]
        public async Task AutomaticReturnsAutomaticPolicy()
        {
            IRegistrationPolicy policy = InteropRegistrationPolicy.Automatic;
            await Assert.That(policy).IsTypeOf<AutomaticRegistrationPolicy>().ConfigureAwait(false);
        }

        [global::TUnit.Core.Test]
        public async Task ExplicitReturnsDefaultPolicyAndIsMarkedObsolete()
        {
            PropertyInfo property = typeof(InteropRegistrationPolicy).GetProperty("Explicit");
            await Assert.That(property).IsNotNull().ConfigureAwait(false);

            IRegistrationPolicy policy = GetExplicitPolicy(property);

            await Assert.That(policy).IsTypeOf<DefaultRegistrationPolicy>().ConfigureAwait(false);
            await Assert.That(IsExplicitPropertyObsolete(property)).IsTrue().ConfigureAwait(false);
        }

        private static IRegistrationPolicy GetExplicitPolicy(PropertyInfo property)
        {
            object value = InvokeObsoleteGetter(property, null);
            return (IRegistrationPolicy)value;
        }

        private static object InvokeObsoleteGetter(PropertyInfo property, object instance)
        {
            MethodInfo getter = property.GetGetMethod(nonPublic: true);
            if (getter == null)
            {
                throw new InvalidOperationException(
                    $"Property {property.Name} must expose a getter."
                );
            }

            return getter.Invoke(instance, Array.Empty<object>());
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
