namespace NovaSharp.Interpreter.Tests.Units
{
    using System;
    using NovaSharp.Interpreter.DataTypes;
    using NovaSharp.Interpreter.Interop;
    using NovaSharp.Interpreter.Interop.RegistrationPolicies;
    using NovaSharp.Interpreter.Interop.UserDataRegistries;
    using NUnit.Framework;

    [TestFixture]
    [Parallelizable(ParallelScope.Self)]
    public sealed class UserDataIsolationTests
    {
        [Test]
        public void IsolationScopeRestoresRegisteredTypes()
        {
            Type isolatedType = typeof(IsolatedType);
            TypeDescriptorRegistry.UnregisterType(isolatedType);

            using (UserData.BeginIsolationScope())
            {
                UserData.RegisterType(isolatedType);
                _ = new IsolatedType();
                Assert.That(TypeDescriptorRegistry.IsTypeRegistered(isolatedType), Is.True);
            }

            Assert.That(TypeDescriptorRegistry.IsTypeRegistered(isolatedType), Is.False);
        }

        [Test]
        public void IsolationScopeRestoresDefaultAccessMode()
        {
            InteropAccessMode original = TypeDescriptorRegistry.DefaultAccessMode;

            using (UserData.BeginIsolationScope())
            {
                TypeDescriptorRegistry.DefaultAccessMode = InteropAccessMode.Reflection;
                Assert.That(
                    TypeDescriptorRegistry.DefaultAccessMode,
                    Is.EqualTo(InteropAccessMode.Reflection)
                );
            }

            Assert.That(TypeDescriptorRegistry.DefaultAccessMode, Is.EqualTo(original));
            TypeDescriptorRegistry.DefaultAccessMode = original;
        }

        [Test]
        public void IsolationScopeRestoresRegistrationPolicy()
        {
            IRegistrationPolicy originalPolicy = TypeDescriptorRegistry.RegistrationPolicy;
            TestRegistrationPolicy customPolicy = new();

            using (UserData.BeginIsolationScope())
            {
                TypeDescriptorRegistry.RegistrationPolicy = customPolicy;
                Assert.That(TypeDescriptorRegistry.RegistrationPolicy, Is.SameAs(customPolicy));
            }

            Assert.That(TypeDescriptorRegistry.RegistrationPolicy, Is.SameAs(originalPolicy));
            TypeDescriptorRegistry.RegistrationPolicy = originalPolicy;
        }

        private sealed class IsolatedType { }

        private sealed class TestRegistrationPolicy : IRegistrationPolicy
        {
            public bool AllowTypeAutoRegistration(Type type) => false;

            public IUserDataDescriptor HandleRegistration(
                IUserDataDescriptor newDescriptor,
                IUserDataDescriptor oldDescriptor
            ) => newDescriptor ?? oldDescriptor;
        }
    }
}
