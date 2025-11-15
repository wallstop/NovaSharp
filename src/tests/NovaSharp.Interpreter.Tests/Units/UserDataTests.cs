namespace NovaSharp.Interpreter.Tests.Units
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using NovaSharp.Interpreter;
    using NovaSharp.Interpreter.DataTypes;
    using NovaSharp.Interpreter.Interop;
    using NovaSharp.Interpreter.Interop.BasicDescriptors;
    using NovaSharp.Interpreter.Interop.RegistrationPolicies;
    using NovaSharp.Interpreter.Interop.StandardDescriptors;
    using NovaSharp.Interpreter.Interop.StandardDescriptors.MemberDescriptors;
    using NUnit.Framework;

    [TestFixture]
    public sealed class UserDataTests
    {
        [TearDown]
        public void Cleanup()
        {
            UserData.UnregisterType(typeof(CustomDescriptorHost));
            UserData.UnregisterType(typeof(HistoricalHost));
            UserData.UnregisterType(typeof(ProxyTarget));
            UserData.UnregisterType(typeof(ProxySurface));
            UserData.RegistrationPolicy = InteropRegistrationPolicy.Default;
        }

        [Test]
        public void CreateReturnsNullForUnregisteredType()
        {
            UserData.UnregisterType(typeof(UnregisteredHost));

            DynValue result = UserData.Create(new UnregisteredHost());

            Assert.That(result, Is.Null);
        }

        [Test]
        public void CustomDescriptorIsUsedForCreationAndWiring()
        {
            CustomWireableDescriptor descriptor = new();
            UserData.RegisterType(descriptor);
            CustomDescriptorHost instance = new CustomDescriptorHost("tracked");

            DynValue dynValue = UserData.Create(instance);
            Table description = UserData.GetDescriptionOfRegisteredTypes();

            Assert.Multiple(() =>
            {
                Assert.That(dynValue.Type, Is.EqualTo(DataType.UserData));
                Assert.That(dynValue.UserData.Object, Is.EqualTo(instance));
                Assert.That(dynValue.UserData.Descriptor, Is.SameAs(descriptor));
                Assert.That(descriptor.PreparedForWiring, Is.True);
                Assert.That(
                    description.Get(descriptor.Type.FullName).Table.Get("name").String,
                    Is.EqualTo(descriptor.Name)
                );
            });
        }

        [Test]
        public void RegisteredTypesHistoryIncludesUnregisteredEntries()
        {
            UserData.RegisterType<HistoricalHost>(InteropAccessMode.Reflection);
            UserData.UnregisterType<HistoricalHost>();

            IEnumerable<Type> current = UserData.GetRegisteredTypes();
            IEnumerable<Type> history = UserData.GetRegisteredTypes(useHistoricalData: true);

            Assert.Multiple(() =>
            {
                Assert.That(current, Does.Not.Contain(typeof(HistoricalHost)));
                Assert.That(history, Does.Contain(typeof(HistoricalHost)));
            });
        }

        [Test]
        public void RegisterProxyTypeRegistersProxyAndTargetDescriptors()
        {
            UserData.RegisterProxyType<ProxySurface, ProxyTarget>(target => new ProxySurface(target));

            DynValue proxied = UserData.Create(new ProxyTarget("proxy"));

            Assert.Multiple(() =>
            {
                Assert.That(UserData.IsTypeRegistered<ProxySurface>(), Is.True);
                Assert.That(UserData.IsTypeRegistered<ProxyTarget>(), Is.True);
                Assert.That(proxied.Type, Is.EqualTo(DataType.UserData));
                Assert.That(proxied.UserData.Descriptor.Type, Is.EqualTo(typeof(ProxyTarget)));
            });
        }

        [Test]
        public void RegisterExtensionTypeExposesMethodsAndIncrementsVersion()
        {
            int startingVersion = UserData.GetExtensionMethodsChangeVersion();
            UserData.RegisterExtensionType(typeof(CustomDescriptorHostExtensions));
            int updatedVersion = UserData.GetExtensionMethodsChangeVersion();

            List<IOverloadableMemberDescriptor> methods = UserData.GetExtensionMethodsByNameAndType(
                nameof(CustomDescriptorHostExtensions.Decorate),
                typeof(CustomDescriptorHost)
            );

            Assert.Multiple(() =>
            {
                Assert.That(updatedVersion, Is.GreaterThan(startingVersion));
                Assert.That(methods, Is.Not.Null);
                Assert.That(methods.Count, Is.GreaterThan(0));
            });
        }

        private sealed class CustomWireableDescriptor
            : IUserDataDescriptor,
                IWireableDescriptor
        {
            public bool PreparedForWiring { get; private set; }

            public string Name => "CustomDescriptorHost";

            public Type Type => typeof(CustomDescriptorHost);

            public DynValue Index(
                Script script,
                object obj,
                DynValue index,
                bool isDirectIndexing
            )
            {
                return DynValue.NewString("indexed");
            }

            public bool SetIndex(
                Script script,
                object obj,
                DynValue index,
                DynValue value,
                bool isDirectIndexing
            )
            {
                return true;
            }

            public string AsString(object obj)
            {
                return $"custom:{obj}";
            }

            public DynValue MetaIndex(Script script, object obj, string metaname)
            {
                return DynValue.Nil;
            }

            public bool IsTypeCompatible(Type type, object obj)
            {
                return type.IsInstanceOfType(obj);
            }

            public void PrepareForWiring(Table t)
            {
                PreparedForWiring = true;
                t.Set("name", DynValue.NewString(Name));
            }
        }

    }

}

internal sealed class CustomDescriptorHost
{
    public CustomDescriptorHost(string name)
    {
        Name = name;
    }

    public string Name { get; }

    public override string ToString()
    {
        return $"Host:{Name}";
    }
}

internal sealed class HistoricalHost
{
}

internal sealed class ProxyTarget
{
    public ProxyTarget(string name)
    {
        Name = name;
    }

    public string Name { get; }
}

internal sealed class ProxySurface
{
    public ProxySurface(ProxyTarget target)
    {
        Target = target;
    }

    public ProxyTarget Target { get; }
}

internal sealed class UnregisteredHost
{
}

internal static class CustomDescriptorHostExtensions
{
    public static string Decorate(this CustomDescriptorHost host, string suffix)
    {
        return (host?.ToString() ?? string.Empty) + suffix;
    }
}
