namespace NovaSharp.Interpreter.Tests.Units
{
    using System;
    using System.Collections.Generic;
    using NovaSharp.Interpreter;
    using NovaSharp.Interpreter.DataTypes;
    using NovaSharp.Interpreter.Interop;
    using NovaSharp.Interpreter.Interop.StandardDescriptors;
    using NUnit.Framework;

    [TestFixture]
    public sealed class StandardGenericsUserDataDescriptorTests
    {
        [Test]
        public void ConstructorThrowsWhenReflectionDisabled()
        {
            Assert.That(
                () =>
                    new StandardGenericsUserDataDescriptor(
                        typeof(List<>),
                        InteropAccessMode.NoReflectionAllowed
                    ),
                Throws
                    .TypeOf<ArgumentException>()
                    .With.Message.Contains("StandardGenericsUserDataDescriptor")
            );
        }

        [Test]
        public void PropertiesReflectInputs()
        {
            StandardGenericsUserDataDescriptor descriptor = new(
                typeof(Dictionary<,>),
                InteropAccessMode.Default
            );

            Assert.Multiple(() =>
            {
                Assert.That(descriptor.Type, Is.EqualTo(typeof(Dictionary<,>)));
                Assert.That(
                    descriptor.Name,
                    Is.EqualTo("@@System.Collections.Generic.Dictionary`2")
                );
                Assert.That(descriptor.AccessMode, Is.EqualTo(InteropAccessMode.Default));
            });
        }

        [Test]
        public void IndexAndMetaIndexReturnNullWhileSetIndexReturnsFalse()
        {
            StandardGenericsUserDataDescriptor descriptor = new(
                typeof(List<>),
                InteropAccessMode.Default
            );
            DynValue result = descriptor.Index(
                new Script(),
                null,
                DynValue.NewString("anything"),
                true
            );

            Assert.Multiple(() =>
            {
                Assert.That(result, Is.Null);
                Assert.That(
                    descriptor.SetIndex(
                        new Script(),
                        null,
                        DynValue.NewString("anything"),
                        DynValue.NewNumber(1),
                        true
                    ),
                    Is.False
                );
                Assert.That(descriptor.MetaIndex(new Script(), null, "__add"), Is.Null);
            });
        }

        [Test]
        public void AsStringUsesUnderlyingObjectToString()
        {
            StandardGenericsUserDataDescriptor descriptor = new(
                typeof(List<>),
                InteropAccessMode.Default
            );

            Assert.That(descriptor.AsString(42), Is.EqualTo("42"));
        }

        [Test]
        public void IsTypeCompatibleDelegatesToFramework()
        {
            StandardGenericsUserDataDescriptor descriptor = new(
                typeof(List<>),
                InteropAccessMode.Default
            );

            Assert.Multiple(() =>
            {
                Assert.That(descriptor.IsTypeCompatible(typeof(object), new object()), Is.True);
                Assert.That(descriptor.IsTypeCompatible(typeof(string), 42), Is.False);
            });
        }

        [Test]
        public void GenerateReturnsNullForOpenGeneric()
        {
            StandardGenericsUserDataDescriptor descriptor = new(
                typeof(List<>),
                InteropAccessMode.Default
            );

            Assert.That(descriptor.Generate(typeof(List<>)), Is.Null);
        }

        [Test]
        public void GenerateReturnsNullWhenTypeAlreadyRegistered()
        {
            UserData.RegisterType(typeof(GenericStub<int>));
            try
            {
                StandardGenericsUserDataDescriptor descriptor = new(
                    typeof(GenericStub<>),
                    InteropAccessMode.Default
                );

                Assert.That(descriptor.Generate(typeof(GenericStub<int>)), Is.Null);
            }
            finally
            {
                UserData.UnregisterType(typeof(GenericStub<int>));
            }
        }

        [Test]
        public void GenerateRegistersUnregisteredConstructedType()
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

                Assert.Multiple(() =>
                {
                    Assert.That(generated, Is.Not.Null);
                    Assert.That(UserData.IsTypeRegistered(concreteType), Is.True);
                });
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
