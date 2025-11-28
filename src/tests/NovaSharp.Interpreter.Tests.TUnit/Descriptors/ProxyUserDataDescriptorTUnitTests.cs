namespace NovaSharp.Interpreter.Tests.TUnit.Descriptors
{
    using System;
    using System.Threading.Tasks;
    using global::TUnit.Assertions;
    using NovaSharp.Interpreter;
    using NovaSharp.Interpreter.DataTypes;
    using NovaSharp.Interpreter.Execution;
    using NovaSharp.Interpreter.Interop;
    using NovaSharp.Interpreter.Interop.ProxyObjects;
    using NovaSharp.Interpreter.Interop.StandardDescriptors;
    using NovaSharp.Interpreter.Tests;

    [ScriptGlobalOptionsIsolation]
    public sealed class ProxyUserDataDescriptorTUnitTests
    {
        private static readonly string[] ExpectedMetaRequests = { "__tostring" };

        [global::TUnit.Core.Test]
        public async Task IndexUsesProxyObjectBeforeDelegating()
        {
            RecordingProxyFactory factory = new();
            RecordingDescriptor inner = new();
            ProxyUserDataDescriptor descriptor = new(factory, inner);
            object target = new Target("inner");
            DynValue index = DynValue.NewString("Key");
            DynValue expected = DynValue.NewString("result");
            inner.IndexResult = expected;

            DynValue value = descriptor.Index(new Script(), target, index, true);

            await Assert.That(value).IsSameReferenceAs(expected);
            await Assert.That(factory.LastInput).IsSameReferenceAs(target);
            await Assert.That(inner.LastObject).IsTypeOf<Proxy>();
            await Assert.That(((Proxy)inner.LastObject).Target).IsSameReferenceAs(target);
            await Assert.That(inner.LastIndex).IsSameReferenceAs(index);
            await Assert.That(inner.LastIsDirectIndexing).IsTrue();
        }

        [global::TUnit.Core.Test]
        public async Task SetIndexReturnsInnerResult()
        {
            RecordingProxyFactory factory = new();
            RecordingDescriptor inner = new();
            ProxyUserDataDescriptor descriptor = new(factory, inner);
            Target target = new("setter");
            DynValue index = DynValue.NewString("name");
            DynValue value = DynValue.NewNumber(5);
            inner.SetIndexResult = true;

            bool handled = descriptor.SetIndex(new Script(), target, index, value, false);

            await Assert.That(handled).IsTrue();
            await Assert.That(inner.LastObject).IsTypeOf<Proxy>();
            await Assert.That(((Proxy)inner.LastObject).Target).IsSameReferenceAs(target);
            await Assert.That(inner.LastValue).IsSameReferenceAs(value);
            await Assert.That(inner.LastIsDirectIndexing).IsFalse();
        }

        [global::TUnit.Core.Test]
        public async Task IndexPassesThroughNullInstancesWithoutProxying()
        {
            RecordingProxyFactory factory = new();
            RecordingDescriptor inner = new();
            ProxyUserDataDescriptor descriptor = new(factory, inner);
            DynValue index = DynValue.NewString("noop");
            inner.IndexResult = DynValue.NewString("result");

            DynValue value = descriptor.Index(new Script(), null, index, true);

            await Assert.That(value.String).IsEqualTo("result");
            await Assert.That(factory.LastInput).IsNull();
            await Assert.That(inner.LastObject).IsNull();
        }

        [global::TUnit.Core.Test]
        public async Task MetaIndexAndAsStringProxyValues()
        {
            RecordingProxyFactory factory = new();
            RecordingDescriptor inner = new();
            ProxyUserDataDescriptor descriptor = new(factory, inner);
            Target target = new("meta");
            DynValue expectedMeta = DynValue.NewString("meta-result");
            inner.MetaIndexResult = expectedMeta;
            inner.AsStringResult = "proxied-meta";

            DynValue metaResult = descriptor.MetaIndex(new Script(), target, "__tostring");
            string asString = descriptor.AsString(target);

            await Assert.That(metaResult).IsSameReferenceAs(expectedMeta);
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
            internal DynValue LastIndex { get; private set; }
            internal DynValue LastValue { get; private set; }
            internal bool LastIsDirectIndexing { get; private set; }
            internal DynValue IndexResult { get; set; } = DynValue.Nil;
            internal bool SetIndexResult { get; set; }
            internal DynValue MetaIndexResult { get; set; } = DynValue.Nil;
            internal string AsStringResult { get; set; } = "<proxy>";
            internal string[] MetaRequests { get; private set; } = Array.Empty<string>();

            public string Name => "recording";
            public Type Type => typeof(Target);

            public DynValue Index(Script script, object obj, DynValue index, bool isDirectIndexing)
            {
                LastObject = obj;
                LastIndex = index;
                LastIsDirectIndexing = isDirectIndexing;
                return IndexResult;
            }

            public bool SetIndex(
                Script script,
                object obj,
                DynValue index,
                DynValue value,
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

            public DynValue MetaIndex(Script script, object obj, string metaname)
            {
                LastObject = obj;
                MetaRequests = new[] { metaname };
                return MetaIndexResult;
            }

            public bool IsTypeCompatible(Type type, object obj)
            {
                return type.IsInstanceOfType(obj);
            }
        }
    }
}
