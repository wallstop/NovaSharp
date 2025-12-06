namespace NovaSharp.Interpreter.Tests.TUnit.Units
{
    using System;
    using System.Threading.Tasks;
    using global::TUnit.Assertions;
    using NovaSharp.Interpreter;
    using NovaSharp.Interpreter.DataTypes;
    using NovaSharp.Interpreter.Interop;
    using NovaSharp.Interpreter.Interop.RegistrationPolicies;

    public sealed class PermanentRegistrationPolicyTUnitTests
    {
        [global::TUnit.Core.Test]
        public async Task HandleRegistrationReturnsNewDescriptorWhenNoExistingDescriptor()
        {
            PermanentRegistrationPolicy policy = new PermanentRegistrationPolicy();
            IUserDataDescriptor newDescriptor = new StubDescriptor(typeof(string));

            IUserDataDescriptor result = policy.HandleRegistration(newDescriptor, null);

            await Assert.That(result).IsEqualTo(newDescriptor);
        }

        [global::TUnit.Core.Test]
        public async Task HandleRegistrationKeepsExistingDescriptor()
        {
            PermanentRegistrationPolicy policy = new PermanentRegistrationPolicy();
            IUserDataDescriptor newDescriptor = new StubDescriptor(typeof(string));
            IUserDataDescriptor existing = new StubDescriptor(typeof(int));

            IUserDataDescriptor result = policy.HandleRegistration(newDescriptor, existing);

            await Assert.That(result).IsEqualTo(existing);
        }

        [global::TUnit.Core.Test]
        public async Task HandleRegistrationWithNullNewDescriptorKeepsExisting()
        {
            PermanentRegistrationPolicy policy = new PermanentRegistrationPolicy();
            IUserDataDescriptor existing = new StubDescriptor(typeof(int));

            IUserDataDescriptor result = policy.HandleRegistration(null, existing);

            await Assert.That(result).IsEqualTo(existing);
        }

        [global::TUnit.Core.Test]
        public async Task AllowTypeAutoRegistrationAlwaysReturnsFalse()
        {
            PermanentRegistrationPolicy policy = new PermanentRegistrationPolicy();

            await Assert.That(policy.AllowTypeAutoRegistration(typeof(object))).IsFalse();
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
