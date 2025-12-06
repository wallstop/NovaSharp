namespace NovaSharp.Interpreter.Tests.TUnit.Descriptors
{
    using System;
    using System.Collections.Generic;
    using System.Threading.Tasks;
    using global::TUnit.Assertions;
    using NovaSharp.Interpreter;
    using NovaSharp.Interpreter.DataTypes;
    using NovaSharp.Interpreter.Interop;
    using NovaSharp.Interpreter.Interop.StandardDescriptors;

    public sealed class CompositeUserDataDescriptorTUnitTests
    {
        [global::TUnit.Core.Test]
        public async Task IndexReturnsFirstNonNullValue()
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

            await Assert.That(value).IsSameReferenceAs(expected);
            await Assert.That(first.IndexCallCount).IsEqualTo(1);
            await Assert.That(second.IndexCallCount).IsEqualTo(1);
        }

        [global::TUnit.Core.Test]
        public async Task IndexStopsIteratingAfterMatch()
        {
            DynValue expected = DynValue.NewString("first");
            StubDescriptor first = new(indexResult: expected);
            StubDescriptor second = new(indexResult: DynValue.Nil);
            CompositeUserDataDescriptor descriptor = CreateComposite(first, second);

            DynValue value = descriptor.Index(
                new Script(),
                new object(),
                DynValue.NewString("name"),
                isDirectIndexing: true
            );

            await Assert.That(value).IsSameReferenceAs(expected);
            await Assert.That(first.IndexCallCount).IsEqualTo(1);
            await Assert.That(second.IndexCallCount).IsEqualTo(0);
        }

        [global::TUnit.Core.Test]
        public async Task IndexReturnsNullWhenDescriptorsReturnNull()
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

            await Assert.That(value).IsNull();
        }

        [global::TUnit.Core.Test]
        public async Task SetIndexStopsAfterFirstHandler()
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

            await Assert.That(handled).IsTrue();
            await Assert.That(first.SetCallCount).IsEqualTo(1);
            await Assert.That(second.SetCallCount).IsEqualTo(0);
        }

        [global::TUnit.Core.Test]
        public async Task SetIndexReturnsFalseWhenAllDescriptorsDecline()
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

            await Assert.That(handled).IsFalse();
        }

        [global::TUnit.Core.Test]
        public async Task MetaIndexReturnsFirstNonNullValue()
        {
            DynValue expected = DynValue.NewString("__call");
            StubDescriptor first = new(indexResult: null, metaResult: null);
            StubDescriptor second = new(indexResult: null, metaResult: expected);
            CompositeUserDataDescriptor descriptor = CreateComposite(first, second);

            DynValue value = descriptor.MetaIndex(new Script(), new object(), "__call");

            await Assert.That(value).IsSameReferenceAs(expected);
            await Assert.That(first.MetaCallCount).IsEqualTo(1);
            await Assert.That(second.MetaCallCount).IsEqualTo(1);
        }

        [global::TUnit.Core.Test]
        public async Task MetaIndexReturnsNullWhenNoDescriptorProvidesMeta()
        {
            CompositeUserDataDescriptor descriptor = CreateComposite(
                new StubDescriptor(indexResult: null, metaResult: null),
                new StubDescriptor(indexResult: null, metaResult: null)
            );

            DynValue value = descriptor.MetaIndex(new Script(), new object(), "__add");

            await Assert.That(value).IsNull();
        }

        [global::TUnit.Core.Test]
        public async Task DescriptorsPropertyIsMutable()
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

            await Assert.That(value).IsSameReferenceAs(expected);
        }

        [global::TUnit.Core.Test]
        public async Task AsStringUsesObjectToString()
        {
            CompositeUserDataDescriptor descriptor = CreateComposite();

            string value = descriptor.AsString(42);
            string nullValue = descriptor.AsString(null);

            await Assert.That(value).IsEqualTo("42");
            await Assert.That(nullValue).IsNull();
        }

        [global::TUnit.Core.Test]
        public async Task NameAndTypeExposeWrappedType()
        {
            CompositeUserDataDescriptor descriptor = CreateComposite(type: typeof(List<int>));

            bool startsWithCaret = descriptor.Name.Length > 0 && descriptor.Name[0] == '^';
            await Assert.That(startsWithCaret).IsTrue();
            await Assert
                .That(
                    descriptor.Name.Contains(typeof(List<int>).FullName, StringComparison.Ordinal)
                )
                .IsTrue();
            await Assert.That(descriptor.Type).IsEqualTo(typeof(List<int>));
        }

        [global::TUnit.Core.Test]
        public async Task IsTypeCompatibleFollowsClrRules()
        {
            CompositeUserDataDescriptor descriptor = CreateComposite();

            await Assert.That(descriptor.IsTypeCompatible(typeof(string), "value")).IsTrue();
            await Assert.That(descriptor.IsTypeCompatible(typeof(string), 17)).IsFalse();
        }

        [global::TUnit.Core.Test]
        public async Task ConstructorThrowsWhenDescriptorsNull()
        {
            ArgumentNullException exception = ExpectException<ArgumentNullException>(() =>
            {
                _ = new CompositeUserDataDescriptor(null, typeof(object));
            });

            await Assert.That(exception.ParamName).IsEqualTo("descriptors");
        }

        [global::TUnit.Core.Test]
        public async Task ConstructorThrowsWhenTypeNull()
        {
            ArgumentNullException exception = ExpectException<ArgumentNullException>(() =>
            {
                _ = new CompositeUserDataDescriptor(new List<IUserDataDescriptor>(), null);
            });

            await Assert.That(exception.ParamName).IsEqualTo("type");
        }

        private static CompositeUserDataDescriptor CreateComposite(
            params StubDescriptor[] descriptors
        ) => CreateComposite(typeof(object), descriptors);

        private static CompositeUserDataDescriptor CreateComposite(
            Type type,
            params StubDescriptor[] descriptors
        )
        {
            List<IUserDataDescriptor> list = new();
            list.AddRange(descriptors);
            return new CompositeUserDataDescriptor(list, type);
        }

        private static TException ExpectException<TException>(Action action)
            where TException : Exception
        {
            try
            {
                action();
            }
            catch (TException exception)
            {
                return exception;
            }

            throw new InvalidOperationException(
                $"Expected exception of type {typeof(TException).Name}."
            );
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
