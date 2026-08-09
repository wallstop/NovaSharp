namespace WallstopStudios.NovaSharp.Interpreter.Tests.TUnit.Descriptors
{
    using System;
    using System.Threading.Tasks;
    using global::NovaSharp;
    using global::TUnit.Assertions;
    using WallstopStudios.NovaSharp.Interpreter;
    using WallstopStudios.NovaSharp.Interpreter.Compatibility;
    using WallstopStudios.NovaSharp.Interpreter.DataTypes;
    using WallstopStudios.NovaSharp.Interpreter.Execution;
    using WallstopStudios.NovaSharp.Interpreter.Interop;
    using WallstopStudios.NovaSharp.Interpreter.Interop.ProxyObjects;
    using WallstopStudios.NovaSharp.Interpreter.Interop.StandardDescriptors;
    using WallstopStudios.NovaSharp.Interpreter.Tests;
    using WallstopStudios.NovaSharp.Tests.TestInfrastructure.TUnit;

    [ScriptGlobalOptionsIsolation]
    public sealed class ProxyUserDataDescriptorTUnitTests
    {
        private static readonly string[] ExpectedMetaRequests = { "__tostring" };

        [global::TUnit.Core.Test]
        [AllLuaVersions]
        public async Task IndexUsesProxyObjectBeforeDelegating(LuaCompatibilityVersion version)
        {
            RecordingProxyFactory factory = new();
            RecordingDescriptor inner = new();
            ProxyUserDataDescriptor descriptor = new(factory, inner);
            object target = new Target("inner");
            LuaValue index = LuaValue.NewString("Key");
            LuaValue expected = LuaValue.NewString("result");
            inner.IndexResult = expected;

            LuaValue value = descriptor.Index(new Script(version), target, index, true).Value;

            await Assert.That(value).IsEqualTo(expected);
            await Assert.That(factory.LastInput).IsSameReferenceAs(target);
            await Assert.That(inner.LastObject).IsTypeOf<Proxy>();
            await Assert.That(((Proxy)inner.LastObject).Target).IsSameReferenceAs(target);
            await Assert.That(inner.LastIndex).IsEqualTo(index);
            await Assert.That(inner.LastIsDirectIndexing).IsTrue();
        }

        [global::TUnit.Core.Test]
        [AllLuaVersions]
        public async Task SetIndexReturnsInnerResult(LuaCompatibilityVersion version)
        {
            RecordingProxyFactory factory = new();
            RecordingDescriptor inner = new();
            ProxyUserDataDescriptor descriptor = new(factory, inner);
            Target target = new("setter");
            LuaValue index = LuaValue.NewString("name");
            LuaValue value = LuaValue.NewNumber(5);
            inner.SetIndexResult = true;

            bool handled = descriptor.SetIndex(new Script(version), target, index, value, false);

            await Assert.That(handled).IsTrue();
            await Assert.That(inner.LastObject).IsTypeOf<Proxy>();
            await Assert.That(((Proxy)inner.LastObject).Target).IsSameReferenceAs(target);
            await Assert.That(inner.LastValue).IsEqualTo(value);
            await Assert.That(inner.LastIsDirectIndexing).IsFalse();
        }

        [global::TUnit.Core.Test]
        [AllLuaVersions]
        public async Task IndexPassesThroughNullInstancesWithoutProxying(
            LuaCompatibilityVersion version
        )
        {
            RecordingProxyFactory factory = new();
            RecordingDescriptor inner = new();
            ProxyUserDataDescriptor descriptor = new(factory, inner);
            LuaValue index = LuaValue.NewString("noop");
            inner.IndexResult = LuaValue.NewString("result");

            LuaValue value = descriptor.Index(new Script(version), null, index, true).Value;

            await Assert.That(value.String).IsEqualTo("result");
            await Assert.That(factory.LastInput).IsNull();
            await Assert.That(inner.LastObject).IsNull();
        }

        [global::TUnit.Core.Test]
        [AllLuaVersions]
        public async Task MetaIndexAndAsStringProxyValues(LuaCompatibilityVersion version)
        {
            RecordingProxyFactory factory = new();
            RecordingDescriptor inner = new();
            ProxyUserDataDescriptor descriptor = new(factory, inner);
            Target target = new("meta");
            LuaValue expectedMeta = LuaValue.NewString("meta-result");
            inner.MetaIndexResult = expectedMeta;
            inner.AsStringResult = "proxied-meta";

            LuaValue metaResult = descriptor
                .MetaIndex(new Script(version), target, "__tostring")
                .Value;
            string asString = descriptor.AsString(target);

            await Assert.That(metaResult).IsEqualTo(expectedMeta);
            await Assert.That(asString).IsEqualTo("proxied-meta");
            await Assert.That(inner.MetaRequests.Length).IsEqualTo(ExpectedMetaRequests.Length);
            await Assert.That(inner.MetaRequests[0]).IsEqualTo(ExpectedMetaRequests[0]);
            await Assert.That(factory.CreatedProxyCount >= 2).IsTrue();
        }

        [global::TUnit.Core.Test]
        public async Task TypeAndNameReflectFactoryAndFriendlyName()
        {
            RecordingProxyFactory factory = new();
            RecordingDescriptor inner = new();
            ProxyUserDataDescriptor descriptor = new(factory, inner, "custom-name");

            await Assert.That(descriptor.Type).IsEqualTo(typeof(Target));
            await Assert.That(descriptor.Name).IsEqualTo("custom-name");
            await Assert.That(descriptor.InnerDescriptor).IsSameReferenceAs(inner);
        }

        [global::TUnit.Core.Test]
        public async Task IsTypeCompatibleUsesFrameworkChecks()
        {
            RecordingProxyFactory factory = new();
            RecordingDescriptor inner = new();
            ProxyUserDataDescriptor descriptor = new(factory, inner);

            await Assert
                .That(descriptor.IsTypeCompatible(typeof(Target), new Target("t")))
                .IsTrue();
            await Assert.That(descriptor.IsTypeCompatible(typeof(Target), new object())).IsFalse();
        }

        private sealed class Target
        {
            internal Target(string name)
            {
                Name = name;
            }

            internal string Name { get; }
        }

        private sealed class Proxy
        {
            internal Proxy(object target)
            {
                Target = target;
            }

            internal object Target { get; }
        }

        private sealed class RecordingProxyFactory : IProxyFactory
        {
            internal object LastInput { get; private set; }
            internal int CreatedProxyCount { get; private set; }

            public Type TargetType => typeof(Target);
            public Type ProxyType => typeof(Proxy);

            public object CreateProxyObject(object o)
            {
                LastInput = o;
                CreatedProxyCount++;
                return new Proxy(o);
            }
        }

        private sealed class RecordingDescriptor : IUserDataDescriptor
        {
            internal object LastObject { get; private set; }
            internal LuaValue LastIndex { get; private set; }
            internal LuaValue LastValue { get; private set; }
            internal bool LastIsDirectIndexing { get; private set; }
            internal LuaValue IndexResult { get; set; } = LuaValue.Nil;
            internal bool SetIndexResult { get; set; }
            internal LuaValue MetaIndexResult { get; set; } = LuaValue.Nil;
            internal string AsStringResult { get; set; } = "<proxy>";
            internal string[] MetaRequests { get; private set; } = Array.Empty<string>();

            public string Name => "recording";
            public Type Type => typeof(Target);

            public bool TryIndex(
                Script script,
                object obj,
                LuaValue index,
                bool isDirectIndexing,
                out LuaValue value
            )
            {
                LastObject = obj;
                LastIndex = index;
                LastIsDirectIndexing = isDirectIndexing;
                value = IndexResult;
                return true;
            }

            public bool SetIndex(
                Script script,
                object obj,
                LuaValue index,
                LuaValue value,
                bool isDirectIndexing
            )
            {
                LastObject = obj;
                LastIndex = index;
                LastValue = value;
                LastIsDirectIndexing = isDirectIndexing;
                return SetIndexResult;
            }

            public string AsString(object obj)
            {
                LastObject = obj;
                return AsStringResult;
            }

            public bool TryMetaIndex(Script script, object obj, string metaname, out LuaValue value)
            {
                LastObject = obj;
                MetaRequests = new[] { metaname };
                value = MetaIndexResult;
                return true;
            }

            public bool IsTypeCompatible(Type type, object obj)
            {
                return type.IsInstanceOfType(obj);
            }
        }
    }
}
