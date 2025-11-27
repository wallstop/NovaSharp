namespace NovaSharp.Interpreter.Tests.Units
{
    using System;
    using NovaSharp.Interpreter;
    using NovaSharp.Interpreter.DataTypes;
    using NovaSharp.Interpreter.Execution;
    using NovaSharp.Interpreter.Interop;
    using NovaSharp.Interpreter.Interop.ProxyObjects;
    using NovaSharp.Interpreter.Interop.StandardDescriptors;
    using NUnit.Framework;

    [TestFixture]
    public sealed class ProxyUserDataDescriptorTests
    {
        private readonly Script _script = new Script();
        private static readonly string[] ExpectedMetaRequests = { "__tostring" };
        private RecordingProxyFactory _factory;
        private RecordingDescriptor _innerDescriptor;
        private ProxyUserDataDescriptor _descriptor;

        [SetUp]
        public void SetUp()
        {
            _factory = new RecordingProxyFactory();
            _innerDescriptor = new RecordingDescriptor();
            _descriptor = new ProxyUserDataDescriptor(_factory, _innerDescriptor);
        }

        [Test]
        public void IndexUsesProxyObjectBeforeDelegating()
        {
            object target = new Target("inner");
            DynValue index = DynValue.NewString("Key");
            DynValue expected = DynValue.NewString("result");
            _innerDescriptor.IndexResult = expected;

            DynValue result = _descriptor.Index(_script, target, index, isDirectIndexing: true);

            Assert.Multiple(() =>
            {
                Assert.That(result, Is.SameAs(expected));
                Assert.That(_factory.LastInput, Is.SameAs(target));
                Assert.That(_innerDescriptor.LastObject, Is.TypeOf<Proxy>());
                Assert.That(((Proxy)_innerDescriptor.LastObject).Target, Is.SameAs(target));
                Assert.That(_innerDescriptor.LastIndex, Is.SameAs(index));
                Assert.That(_innerDescriptor.LastIsDirectIndexing, Is.True);
            });
        }

        [Test]
        public void SetIndexReturnsInnerResult()
        {
            Target target = new("setter");
            DynValue index = DynValue.NewString("name");
            DynValue value = DynValue.NewNumber(5);
            _innerDescriptor.SetIndexResult = true;

            bool result = _descriptor.SetIndex(
                _script,
                target,
                index,
                value,
                isDirectIndexing: false
            );

            Assert.Multiple(() =>
            {
                Assert.That(result, Is.True);
                Assert.That(_innerDescriptor.LastObject, Is.TypeOf<Proxy>());
                Assert.That(((Proxy)_innerDescriptor.LastObject).Target, Is.SameAs(target));
                Assert.That(_innerDescriptor.LastValue, Is.SameAs(value));
                Assert.That(_innerDescriptor.LastIsDirectIndexing, Is.False);
            });
        }

        [Test]
        public void IndexPassesThroughNullInstancesWithoutProxying()
        {
            DynValue index = DynValue.NewString("noop");
            _innerDescriptor.IndexResult = DynValue.NewString("result");

            DynValue result = _descriptor.Index(_script, null, index, isDirectIndexing: true);

            Assert.Multiple(() =>
            {
                Assert.That(result.String, Is.EqualTo("result"));
                Assert.That(_factory.LastInput, Is.Null);
                Assert.That(_innerDescriptor.LastObject, Is.Null);
            });
        }

        [Test]
        public void MetaIndexAndAsStringProxyValues()
        {
            Target target = new("meta");
            DynValue meta = DynValue.NewString("__tostring");
            DynValue expectedMeta = DynValue.NewString("meta-result");
            _innerDescriptor.MetaIndexResult = expectedMeta;
            _innerDescriptor.AsStringResult = "proxied-meta";

            DynValue metaResult = _descriptor.MetaIndex(_script, target, "__tostring");
            string asString = _descriptor.AsString(target);

            Assert.Multiple(() =>
            {
                Assert.That(metaResult, Is.SameAs(expectedMeta));
                Assert.That(asString, Is.EqualTo("proxied-meta"));
                Assert.That(_innerDescriptor.MetaRequests, Is.EqualTo(ExpectedMetaRequests));
                Assert.That(_factory.CreatedProxyCount, Is.GreaterThanOrEqualTo(2));
            });
        }

        [Test]
        public void TypeAndNameReflectFactoryAndFriendlyName()
        {
            ProxyUserDataDescriptor friendlyDescriptor = new ProxyUserDataDescriptor(
                _factory,
                _innerDescriptor,
                "custom-name"
            );

            Assert.Multiple(() =>
            {
                Assert.That(friendlyDescriptor.Type, Is.EqualTo(typeof(Target)));
                Assert.That(friendlyDescriptor.Name, Is.EqualTo("custom-name"));
                Assert.That(friendlyDescriptor.InnerDescriptor, Is.SameAs(_innerDescriptor));
            });
        }

        [Test]
        public void IsTypeCompatibleUsesFrameworkChecks()
        {
            Assert.Multiple(() =>
            {
                Assert.That(_descriptor.IsTypeCompatible(typeof(Target), new Target("t")), Is.True);
                Assert.That(_descriptor.IsTypeCompatible(typeof(Target), new object()), Is.False);
            });
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
