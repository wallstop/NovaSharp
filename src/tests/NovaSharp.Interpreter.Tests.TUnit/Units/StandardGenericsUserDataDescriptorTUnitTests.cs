#pragma warning disable CA2007
namespace NovaSharp.Interpreter.Tests.TUnit.Units
{
    using System;
    using System.Collections.Generic;
    using System.Threading.Tasks;
    using global::TUnit.Assertions;
    using NovaSharp.Interpreter;
    using NovaSharp.Interpreter.DataTypes;
    using NovaSharp.Interpreter.Interop;
    using NovaSharp.Interpreter.Interop.StandardDescriptors;

    [UserDataIsolation]
    public sealed class StandardGenericsUserDataDescriptorTUnitTests
    {
        static StandardGenericsUserDataDescriptorTUnitTests()
        {
            _ = new GenericStub<int>();
        }

        [global::TUnit.Core.Test]
        public void ConstructorThrowsWhenReflectionDisabled()
        {
            Assert.Throws<ArgumentException>(() =>
            {
                StandardGenericsUserDataDescriptor descriptor = new(
                    typeof(List<>),
                    InteropAccessMode.NoReflectionAllowed
                );
                _ = descriptor.Type;
            });
        }

        [global::TUnit.Core.Test]
        public void ConstructorThrowsWhenTypeNull()
        {
            Assert.Throws<ArgumentNullException>(() =>
            {
                StandardGenericsUserDataDescriptor descriptor = new(
                    null,
                    InteropAccessMode.Default
                );
                _ = descriptor.AccessMode;
            });
        }

        [global::TUnit.Core.Test]
        public async Task PropertiesReflectInputs()
        {
            StandardGenericsUserDataDescriptor descriptor = new(
                typeof(Dictionary<,>),
                InteropAccessMode.Default
            );

            await Assert.That(descriptor.Type).IsEqualTo(typeof(Dictionary<,>));
            await Assert
                .That(descriptor.Name)
                .IsEqualTo("@@System.Collections.Generic.Dictionary`2");
            await Assert.That(descriptor.AccessMode).IsEqualTo(InteropAccessMode.Default);
        }

        [global::TUnit.Core.Test]
        public async Task IndexAndMetaIndexReturnNullWhileSetIndexReturnsFalse()
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
                true
            );

            await Assert.That(indexResult).IsNull();
            bool setResult = descriptor.SetIndex(
                script,
                null,
                DynValue.NewString("anything"),
                DynValue.NewNumber(1),
                true
            );
            await Assert.That(setResult).IsFalse();
            await Assert.That(descriptor.MetaIndex(script, null, "__add")).IsNull();
        }

        [global::TUnit.Core.Test]
        public async Task AsStringUsesUnderlyingObjectToString()
        {
            StandardGenericsUserDataDescriptor descriptor = new(
                typeof(List<>),
                InteropAccessMode.Default
            );

            await Assert.That(descriptor.AsString(42)).IsEqualTo("42");
        }

        [global::TUnit.Core.Test]
        public void AsStringThrowsWhenObjectIsNull()
        {
            StandardGenericsUserDataDescriptor descriptor = new(
                typeof(List<>),
                InteropAccessMode.Default
            );

            Assert.Throws<ArgumentNullException>(() => descriptor.AsString(null));
        }

        [global::TUnit.Core.Test]
        public async Task IsTypeCompatibleDelegatesToFramework()
        {
            StandardGenericsUserDataDescriptor descriptor = new(
                typeof(List<>),
                InteropAccessMode.Default
            );

            await Assert.That(descriptor.IsTypeCompatible(typeof(object), new object())).IsTrue();
            await Assert.That(descriptor.IsTypeCompatible(typeof(string), 42)).IsFalse();
        }

        [global::TUnit.Core.Test]
        public async Task GenerateReturnsNullForOpenGeneric()
        {
            StandardGenericsUserDataDescriptor descriptor = new(
                typeof(List<>),
                InteropAccessMode.Default
            );

            await Assert.That(descriptor.Generate(typeof(List<>))).IsNull();
        }

        [global::TUnit.Core.Test]
        public async Task GenerateReturnsNullWhenTypeAlreadyRegistered()
        {
            UserData.RegisterType<GenericStub<int>>();
            try
            {
                StandardGenericsUserDataDescriptor descriptor = new(
                    typeof(GenericStub<>),
                    InteropAccessMode.Default
                );

                await Assert.That(descriptor.Generate(typeof(GenericStub<int>))).IsNull();
            }
            finally
            {
                UserData.UnregisterType<GenericStub<int>>();
            }
        }

        [global::TUnit.Core.Test]
        public async Task GenerateRegistersUnregisteredConstructedType()
        {
            Type concreteType = typeof(GenericStub<string>);
            UserData.UnregisterType(concreteType);

            StandardGenericsUserDataDescriptor descriptor = new(
                typeof(GenericStub<>),
                InteropAccessMode.Default
            );

            try
            {
                IUserDataDescriptor generated = descriptor.Generate(concreteType);

                await Assert.That(generated).IsNotNull();
                await Assert.That(generated.Type).IsEqualTo(concreteType);
                await Assert.That(UserData.IsTypeRegistered(concreteType)).IsTrue();
            }
            finally
            {
                UserData.UnregisterType(concreteType);
            }
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
#pragma warning restore CA2007
