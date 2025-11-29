#pragma warning disable CA2007
namespace NovaSharp.Interpreter.Tests.TUnit.Units
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Text;
    using System.Threading.Tasks;
    using global::TUnit.Assertions;
    using NovaSharp.Interpreter;
    using NovaSharp.Interpreter.DataTypes;
    using NovaSharp.Interpreter.Interop;
    using NovaSharp.Interpreter.Interop.Attributes;
    using NovaSharp.Interpreter.Interop.BasicDescriptors;
    using NovaSharp.Interpreter.Interop.ProxyObjects;
    using NovaSharp.Interpreter.Interop.RegistrationPolicies;
    using NovaSharp.Interpreter.Interop.StandardDescriptors;
    using NovaSharp.Interpreter.Interop.UserDataRegistries;
    using NovaSharp.Interpreter.Tests;

    [UserDataIsolation]
    public sealed class UserDataTUnitTests
    {
        [global::TUnit.Core.Test]
        public async Task CreateReturnsNullForUnregisteredType()
        {
            UserData.UnregisterType<UnregisteredHost>();

            DynValue result = UserData.Create(new UnregisteredHost());

            await Assert.That(result).IsNull();
        }

        [global::TUnit.Core.Test]
        public async Task CustomDescriptorIsUsedForCreationAndWiring()
        {
            CustomWireableDescriptor descriptor = new();
            UserData.RegisterType(descriptor);
            CustomDescriptorHost instance = new("tracked");

            DynValue dynValue = UserData.Create(instance);
            Table description = UserData.GetDescriptionOfRegisteredTypes();

            await Assert.That(dynValue.Type).IsEqualTo(DataType.UserData);
            await Assert.That(dynValue.UserData.Object).IsEqualTo(instance);
            await Assert.That(dynValue.UserData.Descriptor).IsSameReferenceAs(descriptor);
            await Assert.That(descriptor.PreparedForWiring).IsTrue();
            await Assert
                .That(description.Get(descriptor.Type.FullName).Table.Get("name").String)
                .IsEqualTo(descriptor.Name);
        }

        [global::TUnit.Core.Test]
        public async Task RegisteredTypesHistoryIncludesUnregisteredEntries()
        {
            UserData.RegisterType<HistoricalHost>(InteropAccessMode.Reflection);
            UserData.UnregisterType<HistoricalHost>();

            IEnumerable<Type> current = UserData.GetRegisteredTypes();
            IEnumerable<Type> history = UserData.GetRegisteredTypes(useHistoricalData: true);

            await Assert.That(current.Contains(typeof(HistoricalHost))).IsFalse();
            await Assert.That(history.Contains(typeof(HistoricalHost))).IsTrue();
        }

        [global::TUnit.Core.Test]
        public async Task RegisterProxyTypeRegistersProxyAndTargetDescriptors()
        {
            UserData.RegisterProxyType<ProxySurface, ProxyTarget>(target => new ProxySurface(
                target
            ));

            DynValue proxied = UserData.Create(new ProxyTarget("proxy"));

            await Assert.That(UserData.IsTypeRegistered<ProxySurface>()).IsTrue();
            await Assert.That(UserData.IsTypeRegistered<ProxyTarget>()).IsTrue();
            await Assert.That(proxied.Type).IsEqualTo(DataType.UserData);
            await Assert.That(proxied.UserData.Descriptor.Type).IsEqualTo(typeof(ProxyTarget));
        }

        [global::TUnit.Core.Test]
        public async Task RegisterExtensionTypeExposesMethodsAndIncrementsVersion()
        {
            int startingVersion = UserData.GetExtensionMethodsChangeVersion();
            UserData.RegisterExtensionType(typeof(CustomDescriptorHostExtensions));
            int updatedVersion = UserData.GetExtensionMethodsChangeVersion();

            IReadOnlyList<IOverloadableMemberDescriptor> methods =
                UserData.GetExtensionMethodsByNameAndType(
                    nameof(CustomDescriptorHostExtensions.Decorate),
                    typeof(CustomDescriptorHost)
                );

            await Assert.That(updatedVersion).IsGreaterThan(startingVersion);
            await Assert.That(methods).IsNotNull();
            await Assert.That(methods.Count).IsGreaterThan(0);
        }

        [global::TUnit.Core.Test]
        public async Task CreateUsesExplicitDescriptorWhenProvided()
        {
            CustomWireableDescriptor descriptor = new();
            CustomDescriptorHost host = new("explicit");

            DynValue dynValue = UserData.Create(host, descriptor);

            await Assert.That(dynValue.Type).IsEqualTo(DataType.UserData);
            await Assert.That(dynValue.UserData.Descriptor).IsSameReferenceAs(descriptor);
            await Assert.That(dynValue.UserData.Object).IsEqualTo(host);
        }

        [global::TUnit.Core.Test]
        public async Task CreateStaticReturnsNullWhenDescriptorMissing()
        {
            DynValue result = UserData.CreateStatic((IUserDataDescriptor)null);
            await Assert.That(result).IsNull();
        }

        [global::TUnit.Core.Test]
        public async Task CreateStaticReturnsNullWhenDescriptorUnavailable()
        {
            UserData.UnregisterType<UnregisteredHost>();

            DynValue result = UserData.CreateStatic<UnregisteredHost>();

            await Assert.That(result).IsNull();
        }

        [global::TUnit.Core.Test]
        public async Task GetRegisteredTypesIncludesRegisteredDescriptors()
        {
            UserData.RegisterType<RegistryHost>(InteropAccessMode.Reflection);

            IEnumerable<Type> registered = UserData.GetRegisteredTypes();

            await Assert.That(registered.Contains(typeof(RegistryHost))).IsTrue();
        }

        [global::TUnit.Core.Test]
        public async Task AutomaticRegistrationPolicyRegistersTypesOnDemand()
        {
            UserData.UnregisterType<AutoPolicyHost>();

            UserData.RegistrationPolicy = InteropRegistrationPolicy.Automatic;
            try
            {
                DynValue dynValue = UserData.Create(new AutoPolicyHost());
                await Assert.That(dynValue).IsNotNull();
                await Assert.That(UserData.IsTypeRegistered<AutoPolicyHost>()).IsTrue();
            }
            finally
            {
                UserData.RegistrationPolicy = InteropRegistrationPolicy.Default;
            }
        }

        [global::TUnit.Core.Test]
        public async Task RegisterProxyTypeThrowsWhenFactoryIsNull()
        {
            ArgumentNullException exception = Assert.Throws<ArgumentNullException>(() =>
                UserData.RegisterProxyType((IProxyFactory)null)
            );

            await Assert.That(exception.ParamName).IsEqualTo("proxyFactory");
        }

        [global::TUnit.Core.Test]
        public async Task RegisterTypeGenericOverloadRegistersCustomDescriptor()
        {
            CustomWireableDescriptor descriptor = new("generic-overload");

            IUserDataDescriptor result = UserData.RegisterType<CustomDescriptorHost>(descriptor);
            DynValue dynValue = UserData.Create(new CustomDescriptorHost("generic-overload"));

            await Assert.That(result).IsSameReferenceAs(descriptor);
            await Assert.That(dynValue.UserData.Descriptor).IsSameReferenceAs(descriptor);
        }

        [global::TUnit.Core.Test]
        public async Task GetDescriptorForTypeReturnsCompositeWhenBaseAndInterfaceDescriptorsExist()
        {
            IUserDataDescriptor baseDescriptor = UserData.RegisterType<BaseHost>(
                InteropAccessMode.Reflection
            );
            MarkerDescriptor interfaceDescriptor = new();
            UserData.RegisterType<IMarker>(interfaceDescriptor);

            IUserDataDescriptor descriptor = UserData.GetDescriptorForType<DerivedInterfaceHost>(
                searchInterfaces: true
            );

            await Assert.That(descriptor is CompositeUserDataDescriptor).IsTrue();
            CompositeUserDataDescriptor composite = (CompositeUserDataDescriptor)descriptor;
            await Assert.That(composite.Descriptors.Contains(baseDescriptor)).IsTrue();
            await Assert.That(composite.Descriptors.Contains(interfaceDescriptor)).IsTrue();
        }

        [global::TUnit.Core.Test]
        public async Task RegisterTypeKeepsExistingDescriptorUnderDefaultPolicy()
        {
            CustomWireableDescriptor initial = new("initial");
            CustomWireableDescriptor competing = new("competing");

            UserData.RegisterType(initial);
            IUserDataDescriptor result = UserData.RegisterType<CustomDescriptorHost>(competing);
            DynValue dynValue = UserData.Create(new CustomDescriptorHost("policy"));

            await Assert.That(result).IsSameReferenceAs(competing);
            IUserDataDescriptor resolved = UserData.GetDescriptorForType<CustomDescriptorHost>(
                searchInterfaces: false
            );
            await Assert.That(resolved).IsSameReferenceAs(initial);
            await Assert.That(dynValue.UserData.Descriptor).IsSameReferenceAs(initial);
        }

        [global::TUnit.Core.Test]
        public async Task RegisterAssemblyRegistersAnnotatedTypesWhenAssemblyIsNull()
        {
            UserData.UnregisterType<AnnotatedHost>();
            await Assert.That(UserData.IsTypeRegistered<AnnotatedHost>()).IsFalse();

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

            await Assert.That(UserData.IsTypeRegistered<AnnotatedHost>()).IsTrue();
        }

        [global::TUnit.Core.Test]
        public async Task GetDescriptorForTypeGenericReturnsRegisteredDescriptor()
        {
            CustomWireableDescriptor descriptor = new("generic-resolve");
            UserData.RegisterType(descriptor);

            IUserDataDescriptor result = UserData.GetDescriptorForType<CustomDescriptorHost>(
                searchInterfaces: false
            );

            await Assert.That(result).IsSameReferenceAs(descriptor);
        }

        [global::TUnit.Core.Test]
        public async Task GetDescriptionOfRegisteredTypesIncludesHistoricalEntries()
        {
            UserData.RegisterType<HistoricalHost>(InteropAccessMode.Reflection);
            UserData.UnregisterType<HistoricalHost>();

            Table description = UserData.GetDescriptionOfRegisteredTypes(useHistoricalData: true);

            DynValue entry = description.Get(typeof(HistoricalHost).FullName);
            await Assert.That(entry).IsNotNull();
            await Assert.That(entry).IsNotEqualTo(DynValue.Nil);
        }

        [global::TUnit.Core.Test]
        public async Task GetRegisteredTypesHistoricallyIncludesUnregisteredTypes()
        {
            UserData.RegisterType<RegistryHost>(InteropAccessMode.Reflection);
            UserData.UnregisterType<RegistryHost>();

            IEnumerable<Type> history = UserData.GetRegisteredTypes(useHistoricalData: true);

            await Assert.That(history.Contains(typeof(RegistryHost))).IsTrue();
        }

        [global::TUnit.Core.Test]
        public async Task GetDescriptorForObjectFallsBackToBaseType()
        {
            UserData.RegisterType<BaseHost>(InteropAccessMode.Reflection);

            IUserDataDescriptor descriptor = UserData.GetDescriptorForObject(new DerivedHost());

            await Assert.That(descriptor).IsNotNull();
            await Assert.That(descriptor.Type).IsEqualTo(typeof(BaseHost));
        }

        [global::TUnit.Core.Test]
        public async Task UserDataEqualityUsesObjectEquals()
        {
            UserData.RegisterType<EqualityHost>(InteropAccessMode.Reflection);

            DynValue left = UserData.Create(new EqualityHost("value"));
            DynValue right = UserData.Create(new EqualityHost("value"));

            await Assert.That(left.Equals(right)).IsTrue();
        }

        [global::TUnit.Core.Test]
        public async Task UserDataEqualityRequiresMatchingDescriptors()
        {
            CustomWireableDescriptor descriptorA = new();
            CustomWireableDescriptor descriptorB = new();
            CustomDescriptorHost host = new("descriptor");

            DynValue left = UserData.Create(host, descriptorA);
            DynValue right = UserData.Create(host, descriptorB);

            await Assert.That(left.Equals(right)).IsFalse();
        }

        [global::TUnit.Core.Test]
        public async Task StaticUserDataWithMatchingDescriptorsAreEqual()
        {
            UserData.RegisterType<EqualityHost>(InteropAccessMode.Reflection);

            DynValue left = UserData.CreateStatic<EqualityHost>();
            DynValue right = UserData.CreateStatic<EqualityHost>();

            await Assert.That(left.Equals(right)).IsTrue();
        }

        [global::TUnit.Core.Test]
        public async Task UserDataInequalityWhenOnlyOneObjectNull()
        {
            CustomWireableDescriptor descriptor = new();
            CustomDescriptorHost host = new("descriptor");

            DynValue left = UserData.Create(host, descriptor);
            DynValue right = UserData.CreateStatic(descriptor);

            await Assert.That(left.Equals(right)).IsFalse();
        }

        [global::TUnit.Core.Test]
        public async Task CreateReturnsStaticUserDataWhenPassingType()
        {
            UserData.RegisterType<RegistryHost>(InteropAccessMode.Reflection);

            DynValue dynValue = UserData.Create(typeof(RegistryHost));

            await Assert.That(dynValue.Type).IsEqualTo(DataType.UserData);
            await Assert.That(dynValue.UserData.Object).IsNull();
            await Assert.That(dynValue.UserData.Descriptor.Type).IsEqualTo(typeof(RegistryHost));
        }

        [global::TUnit.Core.Test]
        public async Task GetRegisteredTypesIncludesCurrentlyRegisteredTypes()
        {
            UserData.RegisterType<RegistryHost>(InteropAccessMode.Reflection);

            IEnumerable<Type> registered = UserData.GetRegisteredTypes();

            await Assert.That(registered.Contains(typeof(RegistryHost))).IsTrue();
        }

        [global::TUnit.Core.Test]
        public async Task GetDescriptorForTypeSearchInterfacesReturnsInterfaceDescriptor()
        {
            UserData.RegisterType<IMarker>(InteropAccessMode.Reflection);

            IUserDataDescriptor descriptor = UserData.GetDescriptorForType<InterfaceHost>(true);

            await Assert.That(descriptor).IsNotNull();
            await Assert.That(descriptor.Type).IsEqualTo(typeof(IMarker));
        }

        [global::TUnit.Core.Test]
        public async Task RegisterTypeThrowsWhenTypeIsNull()
        {
            ArgumentNullException exception = Assert.Throws<ArgumentNullException>(() =>
                UserData.RegisterType((Type)null)
            );

            await Assert.That(exception.ParamName).IsEqualTo("type");
        }

        [global::TUnit.Core.Test]
        public async Task RegisterTypeWithCustomDescriptorThrowsWhenDescriptorIsNull()
        {
            ArgumentNullException exception = Assert.Throws<ArgumentNullException>(() =>
                UserData.RegisterType(typeof(CustomDescriptorHost), (IUserDataDescriptor)null)
            );

            await Assert.That(exception.ParamName).IsEqualTo("customDescriptor");
        }

        [global::TUnit.Core.Test]
        public async Task RegisterTypeWithCustomDescriptorThrowsWhenTypeIsNull()
        {
            CustomWireableDescriptor descriptor = new();

            ArgumentNullException exception = Assert.Throws<ArgumentNullException>(() =>
                UserData.RegisterType((Type)null, descriptor)
            );

            await Assert.That(exception.ParamName).IsEqualTo("type");
        }

        [global::TUnit.Core.Test]
        public async Task RegisterTypeOverloadThrowsWhenDescriptorIsNull()
        {
            ArgumentNullException exception = Assert.Throws<ArgumentNullException>(() =>
                UserData.RegisterType((IUserDataDescriptor)null)
            );

            await Assert.That(exception.ParamName).IsEqualTo("customDescriptor");
        }

        [global::TUnit.Core.Test]
        public async Task CreateStaticThrowsWhenTypeIsNull()
        {
            ArgumentNullException exception = Assert.Throws<ArgumentNullException>(() =>
                UserData.CreateStatic((Type)null)
            );

            await Assert.That(exception.ParamName).IsEqualTo("t");
        }

        [global::TUnit.Core.Test]
        public async Task GetDescriptorForObjectThrowsWhenValueIsNull()
        {
            ArgumentNullException exception = Assert.Throws<ArgumentNullException>(() =>
                UserData.GetDescriptorForObject(null)
            );

            await Assert.That(exception.ParamName).IsEqualTo("o");
        }

        [global::TUnit.Core.Test]
        public async Task GetDescriptorForTypeThrowsWhenTypeArgumentIsNull()
        {
            ArgumentNullException exception = Assert.Throws<ArgumentNullException>(() =>
                UserData.GetDescriptorForType((Type)null, searchInterfaces: false)
            );

            await Assert.That(exception.ParamName).IsEqualTo("type");
        }

        [global::TUnit.Core.Test]
        public async Task GetExtensionMethodsByNameThrowsWhenNameIsNull()
        {
            ArgumentNullException exception = Assert.Throws<ArgumentNullException>(() =>
                UserData.GetExtensionMethodsByNameAndType(null, typeof(CustomDescriptorHost))
            );

            await Assert.That(exception.ParamName).IsEqualTo("name");
        }

        [global::TUnit.Core.Test]
        public async Task GetExtensionMethodsByNameThrowsWhenExtendedTypeIsNull()
        {
            ArgumentNullException exception = Assert.Throws<ArgumentNullException>(() =>
                UserData.GetExtensionMethodsByNameAndType("Decorate", null)
            );

            await Assert.That(exception.ParamName).IsEqualTo("extendedType");
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
                && string.Equals(Label, other.Label, StringComparison.Ordinal);
        }

        public override int GetHashCode()
        {
            return Label?.GetHashCode(StringComparison.Ordinal) ?? 0;
        }
    }

    [NovaSharpUserData(AccessMode = InteropAccessMode.Reflection)]
    public sealed class AnnotatedHost { }

    internal sealed class CustomWireableDescriptor : IUserDataDescriptor, IWireableDescriptor
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

        public void PrepareForWiring(Table table)
        {
            PreparedForWiring = true;
            table.Set("name", DynValue.NewString(Name));
        }
    }

    internal sealed class MarkerDescriptor : IUserDataDescriptor
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
#pragma warning restore CA2007
