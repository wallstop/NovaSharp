namespace NovaSharp.Interpreter.Tests.Units
{
    using System;
    using System.Collections.Generic;
    using NovaSharp.Interpreter;
    using NovaSharp.Interpreter.DataTypes;
    using NovaSharp.Interpreter.Interop;
    using NovaSharp.Interpreter.Interop.Attributes;
    using NovaSharp.Interpreter.Interop.BasicDescriptors;
    using NovaSharp.Interpreter.Interop.ProxyObjects;
    using NovaSharp.Interpreter.Interop.StandardDescriptors;
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
            UserData.UnregisterType<CustomDescriptorHost>();
            UserData.UnregisterType<HistoricalHost>();
            UserData.UnregisterType<ProxyTarget>();
            UserData.UnregisterType<ProxySurface>();
            UserData.UnregisterType<RegistryHost>();
            UserData.UnregisterType<AutoPolicyHost>();
            UserData.UnregisterType<BaseHost>();
            UserData.UnregisterType<EqualityHost>();
            UserData.UnregisterType<IMarker>();
            UserData.UnregisterType<AnnotatedHost>();
            UserData.RegistrationPolicy = InteropRegistrationPolicy.Default;
        }

        [Test]
        public void CreateReturnsNullForUnregisteredType()
        {
            UserData.UnregisterType<UnregisteredHost>();

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
            UserData.RegisterProxyType<ProxySurface, ProxyTarget>(target => new ProxySurface(
                target
            ));

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

            IReadOnlyList<IOverloadableMemberDescriptor> methods =
                UserData.GetExtensionMethodsByNameAndType(
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
            UserData.UnregisterType<UnregisteredHost>();

            DynValue result = UserData.CreateStatic<UnregisteredHost>();

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
            UserData.UnregisterType<AutoPolicyHost>();

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
        public void RegisterProxyTypeThrowsWhenFactoryIsNull()
        {
            Assert.That(
                () => UserData.RegisterProxyType((IProxyFactory)null),
                Throws.ArgumentNullException.With.Property("ParamName").EqualTo("proxyFactory")
            );
        }

        [Test]
        public void RegisterTypeGenericOverloadRegistersCustomDescriptor()
        {
            CustomWireableDescriptor descriptor = new("generic-overload");

            IUserDataDescriptor result = UserData.RegisterType<CustomDescriptorHost>(descriptor);
            DynValue dynValue = UserData.Create(new CustomDescriptorHost("generic-overload"));

            Assert.Multiple(() =>
            {
                Assert.That(result, Is.SameAs(descriptor));
                Assert.That(dynValue.UserData.Descriptor, Is.SameAs(descriptor));
            });
        }

        [Test]
        public void GetDescriptorForTypeReturnsCompositeWhenBaseAndInterfaceDescriptorsExist()
        {
            IUserDataDescriptor baseDescriptor = UserData.RegisterType<BaseHost>(
                InteropAccessMode.Reflection
            );
            MarkerDescriptor interfaceDescriptor = new();
            UserData.RegisterType<IMarker>(interfaceDescriptor);

            IUserDataDescriptor descriptor = UserData.GetDescriptorForType(
                typeof(DerivedInterfaceHost),
                searchInterfaces: true
            );

            Assert.That(descriptor, Is.InstanceOf<CompositeUserDataDescriptor>());
            CompositeUserDataDescriptor composite = (CompositeUserDataDescriptor)descriptor;

            Assert.Multiple(() =>
            {
                Assert.That(composite.Descriptors, Does.Contain(baseDescriptor));
                Assert.That(composite.Descriptors, Does.Contain(interfaceDescriptor));
            });
        }

        [Test]
        public void RegisterTypeKeepsExistingDescriptorUnderDefaultPolicy()
        {
            CustomWireableDescriptor initial = new("initial");
            CustomWireableDescriptor competing = new("competing");

            UserData.RegisterType(initial);
            IUserDataDescriptor result = UserData.RegisterType(
                typeof(CustomDescriptorHost),
                competing
            );
            DynValue dynValue = UserData.Create(new CustomDescriptorHost("policy"));

            Assert.Multiple(() =>
            {
                Assert.That(result, Is.SameAs(competing));
                Assert.That(
                    UserData.GetDescriptorForType(
                        typeof(CustomDescriptorHost),
                        searchInterfaces: false
                    ),
                    Is.SameAs(initial)
                );
                Assert.That(dynValue.UserData.Descriptor, Is.SameAs(initial));
            });
        }

        [Test]
        public void RegisterAssemblyRegistersAnnotatedTypesWhenAssemblyIsNull()
        {
            UserData.UnregisterType<AnnotatedHost>();
            Assert.That(UserData.IsTypeRegistered<AnnotatedHost>(), Is.False);

            try
            {
                UserData.RegisterAssembly(includeExtensionTypes: true);
            }
            catch (NotSupportedException)
            {
                UserData.RegisterAssembly(
                    typeof(AnnotatedHost).Assembly,
                    includeExtensionTypes: true
                );
            }

            Assert.That(UserData.IsTypeRegistered<AnnotatedHost>(), Is.True);
        }

        [Test]
        public void GetDescriptorForTypeGenericReturnsRegisteredDescriptor()
        {
            CustomWireableDescriptor descriptor = new("generic-resolve");
            UserData.RegisterType(descriptor);

            IUserDataDescriptor result = UserData.GetDescriptorForType<CustomDescriptorHost>(
                searchInterfaces: false
            );

            Assert.That(result, Is.SameAs(descriptor));
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

        [Test]
        public void UserDataEqualityUsesObjectEquals()
        {
            UserData.RegisterType<EqualityHost>(InteropAccessMode.Reflection);

            DynValue left = UserData.Create(new EqualityHost("value"));
            DynValue right = UserData.Create(new EqualityHost("value"));

            Assert.That(left.Equals(right), Is.True);
        }

        [Test]
        public void UserDataEqualityRequiresMatchingDescriptors()
        {
            CustomWireableDescriptor descriptorA = new();
            CustomWireableDescriptor descriptorB = new();
            CustomDescriptorHost host = new("descriptor");

            DynValue left = UserData.Create(host, descriptorA);
            DynValue right = UserData.Create(host, descriptorB);

            Assert.That(left.Equals(right), Is.False);
        }

        [Test]
        public void StaticUserDataWithMatchingDescriptorsAreEqual()
        {
            UserData.RegisterType<EqualityHost>(InteropAccessMode.Reflection);
            DynValue left = UserData.CreateStatic<EqualityHost>();
            DynValue right = UserData.CreateStatic<EqualityHost>();

            Assert.That(left.Equals(right), Is.True);
        }

        [Test]
        public void UserDataInequalityWhenOnlyOneObjectNull()
        {
            CustomWireableDescriptor descriptor = new();
            CustomDescriptorHost host = new("descriptor");

            DynValue left = UserData.Create(host, descriptor);
            DynValue right = UserData.CreateStatic(descriptor);

            Assert.That(left.Equals(right), Is.False);
        }

        [Test]
        public void CreateReturnsStaticUserDataWhenPassingType()
        {
            UserData.RegisterType<RegistryHost>(InteropAccessMode.Reflection);

            DynValue dynValue = UserData.Create(typeof(RegistryHost));

            Assert.Multiple(() =>
            {
                Assert.That(dynValue.Type, Is.EqualTo(DataType.UserData));
                Assert.That(dynValue.UserData.Object, Is.Null);
                Assert.That(dynValue.UserData.Descriptor.Type, Is.EqualTo(typeof(RegistryHost)));
            });
        }

        [Test]
        public void GetRegisteredTypesIncludesCurrentlyRegisteredTypes()
        {
            UserData.RegisterType<RegistryHost>(InteropAccessMode.Reflection);

            IEnumerable<Type> registered = UserData.GetRegisteredTypes();

            Assert.That(registered, Does.Contain(typeof(RegistryHost)));
        }

        [Test]
        public void GetDescriptorForTypeSearchInterfacesReturnsInterfaceDescriptor()
        {
            UserData.RegisterType<IMarker>(InteropAccessMode.Reflection);

            IUserDataDescriptor descriptor = UserData.GetDescriptorForType(
                typeof(InterfaceHost),
                true
            );

            Assert.Multiple(() =>
            {
                Assert.That(descriptor, Is.Not.Null);
                Assert.That(descriptor.Type, Is.EqualTo(typeof(IMarker)));
            });
        }

        private sealed class CustomWireableDescriptor : IUserDataDescriptor, IWireableDescriptor
        {
            public CustomWireableDescriptor(string identifier = null)
            {
                Identifier = identifier;
            }

            public string Identifier { get; }

            public bool PreparedForWiring { get; private set; }

            public string Name => "CustomDescriptorHost";

            public Type Type => typeof(CustomDescriptorHost);

            public DynValue Index(Script script, object obj, DynValue index, bool isDirectIndexing)
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

        private sealed class MarkerDescriptor : IUserDataDescriptor
        {
            public string Name => "IMarker";

            public Type Type => typeof(IMarker);

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
                return obj?.ToString();
            }

            public DynValue MetaIndex(Script script, object obj, string metaname)
            {
                return DynValue.Nil;
            }

            public bool IsTypeCompatible(Type type, object obj)
            {
                return type.IsInstanceOfType(obj);
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

public sealed class HistoricalHost { }

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

internal sealed class UnregisteredHost { }

internal static class CustomDescriptorHostExtensions
{
    public static string Decorate(this CustomDescriptorHost host, string suffix)
    {
        return (host?.ToString() ?? string.Empty) + suffix;
    }
}

public sealed class RegistryHost { }

internal sealed class AutoPolicyHost { }

public class BaseHost { }

internal sealed class DerivedHost : BaseHost { }

public sealed class DerivedInterfaceHost : BaseHost, IMarker { }

internal interface IMarker { }

public sealed class InterfaceHost : IMarker { }

internal sealed class EqualityHost
{
    public EqualityHost(string label)
    {
        Label = label;
    }

    public string Label { get; }

    public override bool Equals(object obj)
    {
        return obj is EqualityHost other
            && string.Equals(Label, other.Label, System.StringComparison.Ordinal);
    }

    public override int GetHashCode()
    {
        return Label?.GetHashCode(System.StringComparison.Ordinal) ?? 0;
    }
}

[global::NovaSharp.Interpreter.Interop.Attributes.NovaSharpUserData(
    AccessMode = global::NovaSharp.Interpreter.Interop.InteropAccessMode.Reflection
)]
public sealed class AnnotatedHost { }
