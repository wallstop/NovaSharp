namespace NovaSharp.Interpreter.Tests.Units
{
    using System;
    using System.Linq;
    using NovaSharp.Interpreter;
    using NovaSharp.Interpreter.DataTypes;
    using NovaSharp.Interpreter.Interop;
    using NUnit.Framework;

    [TestFixture]
    public sealed class UserDataTests
    {
        [Test]
        public void CreateReturnsNullWhenDescriptorIsMissing()
        {
            DynValue result = UserData.Create(new UnregisteredSample());

            Assert.That(result, Is.Null);
        }

        [Test]
        public void CreateWithDescriptorWrapsTheProvidedObject()
        {
            SampleDescriptor descriptor = new(typeof(RegisteredSample), "RegisteredSample");
            RegisteredSample instance = new();

            DynValue dynValue = UserData.Create(instance, descriptor);

            Assert.Multiple(() =>
            {
                Assert.That(dynValue.Type, Is.EqualTo(DataType.UserData));
                Assert.That(dynValue.UserData.Object, Is.SameAs(instance));
                Assert.That(dynValue.UserData.Descriptor, Is.SameAs(descriptor));
            });
        }

        [Test]
        public void RegisterProxyTypeThrowsWhenFactoryIsNull()
        {
            ArgumentNullException exception = Assert.Throws<ArgumentNullException>(
                () => UserData.RegisterProxyType(proxyFactory: null)
            );

            Assert.That(exception.ParamName, Is.EqualTo("proxyFactory"));
        }

        [Test]
        public void GetDescriptionOfRegisteredTypesIncludesWireableDescriptors()
        {
            SampleDescriptor descriptor = new(typeof(WireableSample), "WireableSample");
            UserData.RegisterType(descriptor);

            try
            {
                Table registry = UserData.GetDescriptionOfRegisteredTypes();
                DynValue entry = registry.Get(descriptor.Type.FullName);

                Assert.Multiple(() =>
                {
                    Assert.That(entry.Type, Is.EqualTo(DataType.Table));
                    Assert.That(
                        entry.Table.Get("descriptorName").String,
                        Is.EqualTo("WireableSample")
                    );
                });
            }
            finally
            {
                UserData.UnregisterType(descriptor.Type);
            }
        }

        [Test]
        public void DescriptionIncludesHistoricalDescriptorsOnRequest()
        {
            SampleDescriptor descriptor = new(typeof(HistoricalSample), "HistoricalSample");
            UserData.RegisterType(descriptor);
            UserData.UnregisterType(descriptor.Type);

            Table withoutHistory = UserData.GetDescriptionOfRegisteredTypes();
            Table withHistory = UserData.GetDescriptionOfRegisteredTypes(useHistoricalData: true);

            Assert.Multiple(() =>
            {
                Assert.That(
                    withoutHistory.Get(descriptor.Type.FullName).IsNil(),
                    Is.True,
                    "Active registry should not contain historical descriptors."
                );
                DynValue entry = withHistory.Get(descriptor.Type.FullName);
                Assert.That(entry.Type, Is.EqualTo(DataType.Table));
                Assert.That(
                    entry.Table.Get("typeName").String,
                    Is.EqualTo(typeof(HistoricalSample).FullName)
                );
            });
        }

        [Test]
        public void GetRegisteredTypesHonorsHistoricalFlag()
        {
            SampleDescriptor descriptor = new(typeof(HistoricalSampleTwo), "HistoricalSampleTwo");
            UserData.RegisterType(descriptor);
            UserData.UnregisterType(descriptor.Type);

            Type[] active = UserData.GetRegisteredTypes().ToArray();
            Type[] history = UserData.GetRegisteredTypes(useHistoricalData: true).ToArray();

            Assert.Multiple(() =>
            {
                Assert.That(active, Does.Not.Contain(typeof(HistoricalSampleTwo)));
                Assert.That(history, Does.Contain(typeof(HistoricalSampleTwo)));
            });
        }

        private sealed class SampleDescriptor : IUserDataDescriptor, IWireableDescriptor
        {
            public SampleDescriptor(Type type, string name)
            {
                Type = type;
                Name = name;
            }

            public string Name { get; }

            public Type Type { get; }

            public DynValue Index(
                Script script,
                object obj,
                DynValue index,
                bool isDirectIndexing
            )
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
                return type.IsInstanceOfType(obj);
            }

            public void PrepareForWiring(Table table)
            {
                table.Set("descriptorName", DynValue.NewString(Name));
                table.Set("typeName", DynValue.NewString(Type.FullName));
            }
        }

        private sealed class UnregisteredSample { }

        private sealed class RegisteredSample { }

        private sealed class WireableSample { }

        private sealed class HistoricalSample { }

        private sealed class HistoricalSampleTwo { }
    }
}
