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
    public sealed class CompositeUserDataDescriptorTests
    {
        [Test]
        public void IndexReturnsFirstNonNullValue()
        {
            DynValue expected = DynValue.NewString("hit");
            StubDescriptor first = new(indexResult: null);
            StubDescriptor second = new(indexResult: expected);
            CompositeUserDataDescriptor descriptor = CreateComposite(first, second);

            DynValue value = descriptor.Index(
                new Script(),
                new object(),
                DynValue.NewString("name"),
                true
            );

            Assert.That(value, Is.SameAs(expected));
            Assert.Multiple(() =>
            {
                Assert.That(first.IndexCallCount, Is.EqualTo(1));
                Assert.That(second.IndexCallCount, Is.EqualTo(1));
            });
        }

        [Test]
        public void IndexReturnsNullWhenDescriptorsReturnNull()
        {
            CompositeUserDataDescriptor descriptor = CreateComposite(
                new StubDescriptor(indexResult: null),
                new StubDescriptor(indexResult: null)
            );

            DynValue value = descriptor.Index(
                new Script(),
                new object(),
                DynValue.NewString("missing"),
                true
            );

            Assert.That(value, Is.Null);
        }

        [Test]
        public void SetIndexStopsAfterFirstHandler()
        {
            StubDescriptor first = new(indexResult: null, setResult: true);
            StubDescriptor second = new(indexResult: null, setResult: true);
            CompositeUserDataDescriptor descriptor = CreateComposite(first, second);

            bool handled = descriptor.SetIndex(
                new Script(),
                new object(),
                DynValue.NewString("k"),
                DynValue.True,
                true
            );

            Assert.That(handled, Is.True);
            Assert.Multiple(() =>
            {
                Assert.That(first.SetCallCount, Is.EqualTo(1));
                Assert.That(second.SetCallCount, Is.EqualTo(0));
            });
        }

        [Test]
        public void SetIndexReturnsFalseWhenAllDescriptorsDecline()
        {
            CompositeUserDataDescriptor descriptor = CreateComposite(
                new StubDescriptor(indexResult: null, setResult: false),
                new StubDescriptor(indexResult: null, setResult: false)
            );

            bool handled = descriptor.SetIndex(
                new Script(),
                new object(),
                DynValue.NewString("k"),
                DynValue.True,
                true
            );

            Assert.That(handled, Is.False);
        }

        [Test]
        public void MetaIndexReturnsFirstNonNullValue()
        {
            DynValue expected = DynValue.NewString("__call");
            StubDescriptor first = new(indexResult: null, metaResult: null);
            StubDescriptor second = new(indexResult: null, metaResult: expected);
            CompositeUserDataDescriptor descriptor = CreateComposite(first, second);

            DynValue value = descriptor.MetaIndex(new Script(), new object(), "__call");

            Assert.That(value, Is.SameAs(expected));
            Assert.Multiple(() =>
            {
                Assert.That(first.MetaCallCount, Is.EqualTo(1));
                Assert.That(second.MetaCallCount, Is.EqualTo(1));
            });
        }

        [Test]
        public void DescriptorsPropertyIsMutable()
        {
            CompositeUserDataDescriptor descriptor = CreateComposite();
            DynValue expected = DynValue.NewNumber(5);

            descriptor.Descriptors.Add(new StubDescriptor(indexResult: expected));

            DynValue value = descriptor.Index(
                new Script(),
                new object(),
                DynValue.NewString("value"),
                true
            );

            Assert.That(value, Is.SameAs(expected));
        }

        [Test]
        public void AsStringUsesObjectToString()
        {
            CompositeUserDataDescriptor descriptor = CreateComposite();

            string value = descriptor.AsString(42);
            string nullValue = descriptor.AsString(null);

            Assert.That(value, Is.EqualTo("42"));
            Assert.That(nullValue, Is.Null);
        }

        [Test]
        public void IsTypeCompatibleFollowsClrRules()
        {
            CompositeUserDataDescriptor descriptor = CreateComposite();

            Assert.Multiple(() =>
            {
                Assert.That(descriptor.IsTypeCompatible(typeof(string), "value"), Is.True);
                Assert.That(descriptor.IsTypeCompatible(typeof(string), 17), Is.False);
            });
        }

        private static CompositeUserDataDescriptor CreateComposite(
            params StubDescriptor[] descriptors
        )
        {
            List<IUserDataDescriptor> list = new();
            list.AddRange(descriptors);
            return new CompositeUserDataDescriptor(list, typeof(object));
        }

        private sealed class StubDescriptor : IUserDataDescriptor
        {
            private readonly DynValue _indexResult;
            private readonly bool _setResult;
            private readonly DynValue _metaResult;

            public StubDescriptor(
                DynValue indexResult,
                bool setResult = false,
                DynValue metaResult = null
            )
            {
                _indexResult = indexResult;
                _setResult = setResult;
                _metaResult = metaResult;
            }

            public int IndexCallCount { get; private set; }
            public int SetCallCount { get; private set; }
            public int MetaCallCount { get; private set; }

            public string Name => "stub";

            public Type Type => typeof(object);

            public DynValue Index(Script script, object obj, DynValue index, bool isDirectIndexing)
            {
                IndexCallCount++;
                return _indexResult;
            }

            public bool SetIndex(
                Script script,
                object obj,
                DynValue index,
                DynValue value,
                bool isDirectIndexing
            )
            {
                SetCallCount++;
                return _setResult;
            }

            public string AsString(object obj)
            {
                return obj?.ToString();
            }

            public DynValue MetaIndex(Script script, object obj, string metaname)
            {
                MetaCallCount++;
                return _metaResult;
            }

            public bool IsTypeCompatible(Type type, object obj)
            {
                return type.IsInstanceOfType(obj);
            }
        }
    }
}
