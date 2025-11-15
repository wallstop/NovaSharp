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
        [SetUp]
        public void ResetPolicy()
        {
            UserData.RegistrationPolicy = InteropRegistrationPolicy.Default;
        }

        [TearDown]
        public void Cleanup()
        {
            UserData.UnregisterType(typeof(CustomDescriptorHost));
            UserData.UnregisterType(typeof(HistoricalHost));
            UserData.UnregisterType(typeof(ProxyTarget));
            UserData.UnregisterType(typeof(ProxySurface));
            UserData.UnregisterType(typeof(RegistryHost));
            UserData.UnregisterType(typeof(AutoPolicyHost));
            UserData.UnregisterType(typeof(BaseHost));
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

        [Test]
        public void CreateUsesExplicitDescriptorWhenProvided()
        {
            CustomWireableDescriptor descriptor = new();
            CustomDescriptorHost host = new("explicit");

            DynValue dynValue = UserData.Create(host, descriptor);

            Assert.Multiple(() =>
            {
                Assert.That(dynValue.Type, Is.EqualTo(DataType.UserData));
                Assert.That(dynValue.UserData.Descriptor, Is.SameAs(descriptor));
                Assert.That(dynValue.UserData.Object, Is.EqualTo(host));
            });
        }

        [Test]
        public void CreateStaticReturnsNullWhenDescriptorMissing()
        {
            Assert.That(UserData.CreateStatic((IUserDataDescriptor)null), Is.Null);
        }

        [Test]
        public void CreateStaticReturnsNullWhenDescriptorUnavailable()
        {
            UserData.UnregisterType(typeof(UnregisteredHost));

            DynValue result = UserData.CreateStatic(typeof(UnregisteredHost));

            Assert.That(result, Is.Null);
        }

        [Test]
        public void GetRegisteredTypesIncludesRegisteredDescriptors()
        {
            UserData.RegisterType<RegistryHost>(InteropAccessMode.Reflection);

            IEnumerable<Type> registered = UserData.GetRegisteredTypes();

            Assert.That(registered, Does.Contain(typeof(RegistryHost)));
        }

        [Test]
        public void AutomaticRegistrationPolicyRegistersTypesOnDemand()
        {
            UserData.UnregisterType(typeof(AutoPolicyHost));

            UserData.RegistrationPolicy = InteropRegistrationPolicy.Automatic;
            DynValue dynValue = UserData.Create(new AutoPolicyHost());
            UserData.RegistrationPolicy = InteropRegistrationPolicy.Default;

            Assert.Multiple(() =>
            {
                Assert.That(dynValue, Is.Not.Null);
                Assert.That(UserData.IsTypeRegistered<AutoPolicyHost>(), Is.True);
            });
        }

        [Test]
        public void GetDescriptionOfRegisteredTypesIncludesHistoricalEntries()
        {
            UserData.RegisterType<HistoricalHost>(InteropAccessMode.Reflection);
            UserData.UnregisterType<HistoricalHost>();

            Table description = UserData.GetDescriptionOfRegisteredTypes(useHistoricalData: true);

            Assert.That(
                description.Get(typeof(HistoricalHost).FullName),
                Is.Not.Null.And.Not.EqualTo(DynValue.Nil)
            );
        }

        [Test]
        public void GetRegisteredTypesHistoricallyIncludesUnregisteredTypes()
        {
            UserData.RegisterType<RegistryHost>(InteropAccessMode.Reflection);
            UserData.UnregisterType<RegistryHost>();

            IEnumerable<Type> history = UserData.GetRegisteredTypes(useHistoricalData: true);

            Assert.That(history, Does.Contain(typeof(RegistryHost)));
        }

        [Test]
        public void GetDescriptorForObjectFallsBackToBaseType()
        {
            UserData.RegisterType<BaseHost>(InteropAccessMode.Reflection);

            IUserDataDescriptor descriptor = UserData.GetDescriptorForObject(new DerivedHost());

            Assert.That(descriptor, Is.Not.Null);
            Assert.That(descriptor.Type, Is.EqualTo(typeof(BaseHost)));
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

internal sealed class RegistryHost
{
}

internal sealed class AutoPolicyHost
{
}

internal class BaseHost
{
}

internal sealed class DerivedHost : BaseHost
{
}
