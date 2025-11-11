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
        public void NameTypeAndDescriptorsSurfaceOriginalValues()
        {
            List<IUserDataDescriptor> inner = new();
            CompositeUserDataDescriptor descriptor = new(inner, typeof(string));

            Assert.Multiple(() =>
            {
                Assert.That(descriptor.Name, Is.EqualTo("^" + typeof(string).FullName));
                Assert.That(descriptor.Type, Is.EqualTo(typeof(string)));
                Assert.That(descriptor.Descriptors, Is.SameAs(inner));
            });
        }

        [Test]
        public void IndexReturnsFirstNonNullResult()
        {
            FakeDescriptor first = new() { IndexResult = null };
            FakeDescriptor second = new() { IndexResult = DynValue.NewString("winner") };

            CompositeUserDataDescriptor descriptor = new(
                new List<IUserDataDescriptor> { first, second },
                typeof(object)
            );

            DynValue result = descriptor.Index(null, new object(), DynValue.NewString("key"), true);

            Assert.Multiple(() =>
            {
                Assert.That(first.IndexCallCount, Is.EqualTo(1));
                Assert.That(second.IndexCallCount, Is.EqualTo(1));
                Assert.That(result.String, Is.EqualTo("winner"));
            });
        }

        [Test]
        public void IndexReturnsNullWhenAllDescriptorsFail()
        {
            FakeDescriptor first = new() { IndexResult = null };
            FakeDescriptor second = new() { IndexResult = null };

            CompositeUserDataDescriptor descriptor = new(
                new List<IUserDataDescriptor> { first, second },
                typeof(object)
            );

            DynValue result = descriptor.Index(
                null,
                new object(),
                DynValue.NewString("missing"),
                true
            );

            Assert.Multiple(() =>
            {
                Assert.That(first.IndexCallCount, Is.EqualTo(1));
                Assert.That(second.IndexCallCount, Is.EqualTo(1));
                Assert.That(result, Is.Null);
            });
        }

        [Test]
        public void SetIndexStopsOnFirstTrue()
        {
            FakeDescriptor first = new() { SetIndexResult = false };
            FakeDescriptor second = new() { SetIndexResult = true };
            FakeDescriptor third = new() { SetIndexResult = true };

            CompositeUserDataDescriptor descriptor = new(
                new List<IUserDataDescriptor> { first, second, third },
                typeof(object)
            );

            bool handled = descriptor.SetIndex(
                null,
                new object(),
                DynValue.NewString("key"),
                DynValue.NewNumber(1),
                true
            );

            Assert.Multiple(() =>
            {
                Assert.That(first.SetIndexCallCount, Is.EqualTo(1));
                Assert.That(second.SetIndexCallCount, Is.EqualTo(1));
                Assert.That(third.SetIndexCallCount, Is.EqualTo(0));
                Assert.That(handled, Is.True);
            });
        }

        [Test]
        public void MetaIndexReturnsFirstNonNullResult()
        {
            FakeDescriptor first = new() { MetaIndexResult = null };
            FakeDescriptor second = new() { MetaIndexResult = DynValue.NewBoolean(true) };

            CompositeUserDataDescriptor descriptor = new(
                new List<IUserDataDescriptor> { first, second },
                typeof(object)
            );

            DynValue meta = descriptor.MetaIndex(null, new object(), "__pairs");

            Assert.Multiple(() =>
            {
                Assert.That(first.MetaIndexCallCount, Is.EqualTo(1));
                Assert.That(second.MetaIndexCallCount, Is.EqualTo(1));
                Assert.That(meta.Boolean, Is.True);
            });
        }

        [Test]
        public void AsStringMirrorsObjectToString()
        {
            CompositeUserDataDescriptor descriptor = new(
                new List<IUserDataDescriptor>(),
                typeof(object)
            );

            string representation = descriptor.AsString(new CustomToString());

            Assert.That(representation, Is.EqualTo("custom-to-string"));
        }

        [Test]
        public void IsTypeCompatibleUsesFrameworkCheck()
        {
            CompositeUserDataDescriptor descriptor = new(
                new List<IUserDataDescriptor>(),
                typeof(object)
            );

            Assert.Multiple(() =>
            {
                Assert.That(descriptor.IsTypeCompatible(typeof(string), "value"), Is.True);
                Assert.That(descriptor.IsTypeCompatible(typeof(string), 42), Is.False);
            });
        }

        private sealed class CustomToString
        {
            public override string ToString()
            {
                return "custom-to-string";
            }
        }

        private sealed class FakeDescriptor : IUserDataDescriptor
        {
            public DynValue IndexResult { get; set; }

            public bool SetIndexResult { get; set; }

            public DynValue MetaIndexResult { get; set; }

            public int IndexCallCount { get; private set; }

            public int SetIndexCallCount { get; private set; }

            public int MetaIndexCallCount { get; private set; }

            public string Name
            {
                get { return "fake"; }
            }

            public Type Type
            {
                get { return typeof(object); }
            }

            public DynValue Index(Script script, object obj, DynValue index, bool isDirectIndexing)
            {
                IndexCallCount++;
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
                SetIndexCallCount++;
                return SetIndexResult;
            }

            public string AsString(object obj)
            {
                return obj?.ToString();
            }

            public DynValue MetaIndex(Script script, object obj, string metaname)
            {
                MetaIndexCallCount++;
                return MetaIndexResult;
            }

            public bool IsTypeCompatible(Type type, object obj)
            {
                return type.IsInstanceOfType(obj);
            }
        }
    }
}
