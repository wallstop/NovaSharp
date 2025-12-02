namespace NovaSharp.Interpreter.Tests.TUnit.Descriptors
{
    using System;
    using System.Collections.Generic;
    using System.Threading.Tasks;
    using global::TUnit.Assertions;
    using NovaSharp.Interpreter;
    using NovaSharp.Interpreter.DataTypes;
    using NovaSharp.Interpreter.Interop;
    using NovaSharp.Interpreter.Interop.StandardDescriptors;
    using NovaSharp.Interpreter.Tests;
    using NovaSharp.Tests.TestInfrastructure.Scopes;

    [UserDataIsolation]
    public sealed class StandardGenericsUserDataDescriptorTUnitTests
    {
        static StandardGenericsUserDataDescriptorTUnitTests()
        {
            _ = new GenericStub<int>();
        }

        [global::TUnit.Core.Test]
        public async Task ConstructorThrowsWhenReflectionDisabled()
        {
            ArgumentException exception = Assert.Throws<ArgumentException>(() =>
            {
                StandardGenericsUserDataDescriptor descriptor = new(
                    typeof(List<>),
                    InteropAccessMode.NoReflectionAllowed
                );
                _ = descriptor;
            });

            await Assert
                .That(exception.Message)
                .Contains("StandardGenericsUserDataDescriptor")
                .ConfigureAwait(false);
        }

        [global::TUnit.Core.Test]
        public async Task ConstructorThrowsWhenTypeNull()
        {
            ArgumentNullException exception = Assert.Throws<ArgumentNullException>(() =>
            {
                StandardGenericsUserDataDescriptor descriptor = new(
                    null,
                    InteropAccessMode.Default
                );
                _ = descriptor;
            });

            await Assert.That(exception.ParamName).IsEqualTo("type").ConfigureAwait(false);
        }

        [global::TUnit.Core.Test]
        public async Task PropertiesReflectInputs()
        {
            StandardGenericsUserDataDescriptor descriptor = new(
                typeof(Dictionary<,>),
                InteropAccessMode.Default
            );

            await Assert
                .That(descriptor.Type)
                .IsEqualTo(typeof(Dictionary<,>))
                .ConfigureAwait(false);
            await Assert
                .That(descriptor.Name)
                .IsEqualTo("@@System.Collections.Generic.Dictionary`2")
                .ConfigureAwait(false);
            await Assert
                .That(descriptor.AccessMode)
                .IsEqualTo(InteropAccessMode.Default)
                .ConfigureAwait(false);
        }

        [global::TUnit.Core.Test]
        public async Task IndexMetaIndexAndSetIndexReturnDefaults()
        {
            StandardGenericsUserDataDescriptor descriptor = new(
                typeof(List<>),
                InteropAccessMode.Default
            );
            Script script = new();

            DynValue indexResult = descriptor.Index(
                script,
                null,
                DynValue.NewString("anything"),
                isDirectIndexing: true
            );

            bool setIndexResult = descriptor.SetIndex(
                script,
                null,
                DynValue.NewString("anything"),
                DynValue.NewNumber(1),
                isDirectIndexing: true
            );

            DynValue metaIndexResult = descriptor.MetaIndex(script, null, "__add");

            await Assert.That(indexResult).IsNull().ConfigureAwait(false);
            await Assert.That(setIndexResult).IsFalse().ConfigureAwait(false);
            await Assert.That(metaIndexResult).IsNull().ConfigureAwait(false);
        }

        [global::TUnit.Core.Test]
        public async Task AsStringUsesUnderlyingObjectToString()
        {
            StandardGenericsUserDataDescriptor descriptor = new(
                typeof(List<>),
                InteropAccessMode.Default
            );

            await Assert.That(descriptor.AsString(42)).IsEqualTo("42").ConfigureAwait(false);
        }

        [global::TUnit.Core.Test]
        public async Task AsStringThrowsWhenObjectIsNull()
        {
            StandardGenericsUserDataDescriptor descriptor = new(
                typeof(List<>),
                InteropAccessMode.Default
            );

            ArgumentNullException exception = Assert.Throws<ArgumentNullException>(() =>
                descriptor.AsString(null)
            );

            await Assert.That(exception.ParamName).IsEqualTo("obj").ConfigureAwait(false);
        }

        [global::TUnit.Core.Test]
        public async Task IsTypeCompatibleDelegatesToFramework()
        {
            StandardGenericsUserDataDescriptor descriptor = new(
                typeof(List<>),
                InteropAccessMode.Default
            );

            await Assert
                .That(descriptor.IsTypeCompatible(typeof(object), new object()))
                .IsTrue()
                .ConfigureAwait(false);
            await Assert
                .That(descriptor.IsTypeCompatible(typeof(string), 42))
                .IsFalse()
                .ConfigureAwait(false);
        }

        [global::TUnit.Core.Test]
        public async Task GenerateReturnsNullForOpenGeneric()
        {
            StandardGenericsUserDataDescriptor descriptor = new(
                typeof(List<>),
                InteropAccessMode.Default
            );

            await Assert.That(descriptor.Generate(typeof(List<>))).IsNull().ConfigureAwait(false);
        }

        [global::TUnit.Core.Test]
        public async Task GenerateReturnsNullWhenTypeAlreadyRegistered()
        {
            using UserDataRegistrationScope registrationScope = UserDataRegistrationScope.Track<
                GenericStub<int>
            >(ensureUnregistered: true);

            registrationScope.RegisterType<GenericStub<int>>();

            StandardGenericsUserDataDescriptor descriptor = new(
                typeof(GenericStub<>),
                InteropAccessMode.Default
            );

            await Assert
                .That(descriptor.Generate(typeof(GenericStub<int>)))
                .IsNull()
                .ConfigureAwait(false);
        }

        [global::TUnit.Core.Test]
        public async Task GenerateRegistersUnregisteredConstructedType()
        {
            Type concreteType = typeof(GenericStub<string>);
            using UserDataRegistrationScope registrationScope = UserDataRegistrationScope.Track(
                concreteType,
                ensureUnregistered: true
            );

            StandardGenericsUserDataDescriptor descriptor = new(
                typeof(GenericStub<>),
                InteropAccessMode.Default
            );

            IUserDataDescriptor generated = descriptor.Generate(concreteType);

            await Assert.That(generated).IsNotNull().ConfigureAwait(false);
            await Assert.That(generated.Type).IsEqualTo(concreteType).ConfigureAwait(false);
            await Assert
                .That(UserData.IsTypeRegistered(concreteType))
                .IsTrue()
                .ConfigureAwait(false);
        }

        private sealed class GenericStub<T>
        {
            public override string ToString()
            {
                return $"GenericStub<{typeof(T).Name}>";
            }
        }
    }
}
