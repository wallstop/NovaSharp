namespace NovaSharp.Interpreter.Tests.Units
{
    using System;
    using NovaSharp.Interpreter;
    using NovaSharp.Interpreter.DataTypes;
    using NovaSharp.Interpreter.Interop;
    using NovaSharp.Interpreter.Interop.RegistrationPolicies;
    using NUnit.Framework;

    [TestFixture]
    public sealed class PermanentRegistrationPolicyTests
    {
        [Test]
        public void HandleRegistrationReturnsNewDescriptorWhenNoExistingDescriptor()
        {
            PermanentRegistrationPolicy policy = new PermanentRegistrationPolicy();
            IUserDataDescriptor newDescriptor = new StubDescriptor(typeof(string));

            IUserDataDescriptor result = policy.HandleRegistration(newDescriptor, null);

            Assert.That(result, Is.EqualTo(newDescriptor));
        }

        [Test]
        public void HandleRegistrationKeepsExistingDescriptor()
        {
            PermanentRegistrationPolicy policy = new PermanentRegistrationPolicy();
            IUserDataDescriptor newDescriptor = new StubDescriptor(typeof(string));
            IUserDataDescriptor existing = new StubDescriptor(typeof(int));

            IUserDataDescriptor result = policy.HandleRegistration(newDescriptor, existing);

            Assert.That(result, Is.EqualTo(existing));
        }

        [Test]
        public void HandleRegistrationWithNullNewDescriptorKeepsExisting()
        {
            PermanentRegistrationPolicy policy = new PermanentRegistrationPolicy();
            IUserDataDescriptor existing = new StubDescriptor(typeof(int));

            IUserDataDescriptor result = policy.HandleRegistration(null, existing);

            Assert.That(result, Is.EqualTo(existing));
        }

        [Test]
        public void AllowTypeAutoRegistrationAlwaysReturnsFalse()
        {
            PermanentRegistrationPolicy policy = new PermanentRegistrationPolicy();

            Assert.That(policy.AllowTypeAutoRegistration(typeof(object)), Is.False);
        }

        private sealed class StubDescriptor : IUserDataDescriptor
        {
            public StubDescriptor(Type type)
            {
                Type = type;
                Name = type.Name;
            }

            public string Name { get; }

            public Type Type { get; }

            public DynValue Index(Script script, object obj, DynValue index, bool isDirectIndexing)
            {
                return DynValue.Nil;
            }

            public bool SetIndex(
                Script script,
                object obj,
                DynValue index,
                DynValue value,
                bool isDirectIndexing
            )
            {
                return false;
            }

            public string AsString(object obj)
            {
                return obj?.ToString() ?? string.Empty;
            }

            public DynValue MetaIndex(Script script, object obj, string metaname)
            {
                return DynValue.Nil;
            }

            public bool IsTypeCompatible(Type type, object obj)
            {
                return obj == null || type.IsInstanceOfType(obj);
            }
        }
    }
}
