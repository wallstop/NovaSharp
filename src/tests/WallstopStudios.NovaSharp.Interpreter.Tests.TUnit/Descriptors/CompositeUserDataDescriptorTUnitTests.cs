namespace WallstopStudios.NovaSharp.Interpreter.Tests.TUnit.Descriptors
{
    using System;
    using System.Collections.Generic;
    using System.Threading.Tasks;
    using global::NovaSharp;
    using global::TUnit.Assertions;
    using WallstopStudios.NovaSharp.Interpreter;
    using WallstopStudios.NovaSharp.Interpreter.Compatibility;
    using WallstopStudios.NovaSharp.Interpreter.DataTypes;
    using WallstopStudios.NovaSharp.Interpreter.Interop;
    using WallstopStudios.NovaSharp.Interpreter.Interop.StandardDescriptors;
    using WallstopStudios.NovaSharp.Tests.TestInfrastructure.TUnit;

    public sealed class CompositeUserDataDescriptorTUnitTests
    {
        [global::TUnit.Core.Test]
        [AllLuaVersions]
        public async Task IndexReturnsFirstNonNullValue(LuaCompatibilityVersion version)
        {
            LuaValue expected = LuaValue.NewString("hit");
            StubDescriptor first = new(indexResult: null);
            StubDescriptor second = new(indexResult: expected);
            CompositeUserDataDescriptor descriptor = CreateComposite(first, second);

            LuaValue value = descriptor
                .Index(new Script(version), new object(), LuaValue.NewString("name"), true)
                .Value;

            await Assert.That(value).IsEqualTo(expected);
            await Assert.That(first.IndexCallCount).IsEqualTo(1);
            await Assert.That(second.IndexCallCount).IsEqualTo(1);
        }

        [global::TUnit.Core.Test]
        [AllLuaVersions]
        public async Task IndexStopsIteratingAfterMatch(LuaCompatibilityVersion version)
        {
            StubDescriptor first = new(indexResult: LuaValue.Nil);
            StubDescriptor second = new(indexResult: LuaValue.Nil);
            CompositeUserDataDescriptor descriptor = CreateComposite(first, second);

            LuaValue value = descriptor
                .Index(
                    new Script(version),
                    new object(),
                    LuaValue.NewString("name"),
                    isDirectIndexing: true
                )
                .Value;

            await Assert.That(value.IsNil).IsTrue();
            await Assert.That(first.IndexCallCount).IsEqualTo(1);
            await Assert.That(second.IndexCallCount).IsEqualTo(0);
        }

        [global::TUnit.Core.Test]
        [AllLuaVersions]
        public async Task IndexReturnsNullWhenDescriptorsReturnNull(LuaCompatibilityVersion version)
        {
            CompositeUserDataDescriptor descriptor = CreateComposite(
                new StubDescriptor(indexResult: null),
                new StubDescriptor(indexResult: null)
            );

            LuaValue? value = descriptor.Index(
                new Script(version),
                new object(),
                LuaValue.NewString("missing"),
                true
            );
            bool found = descriptor.TryIndex(
                new Script(version),
                new object(),
                LuaValue.NewString("missing"),
                true,
                out LuaValue missing
            );

            await Assert.That(value).IsNull();
            await Assert.That(found).IsFalse();
            await Assert.That(missing.IsNil).IsTrue();
        }

        [global::TUnit.Core.Test]
        [AllLuaVersions]
        public async Task SetIndexStopsAfterFirstHandler(LuaCompatibilityVersion version)
        {
            StubDescriptor first = new(indexResult: null, setResult: true);
            StubDescriptor second = new(indexResult: null, setResult: true);
            CompositeUserDataDescriptor descriptor = CreateComposite(first, second);

            bool handled = descriptor.SetIndex(
                new Script(version),
                new object(),
                LuaValue.NewString("k"),
                LuaValue.True,
                true
            );

            await Assert.That(handled).IsTrue();
            await Assert.That(first.SetCallCount).IsEqualTo(1);
            await Assert.That(second.SetCallCount).IsEqualTo(0);
        }

        [global::TUnit.Core.Test]
        [AllLuaVersions]
        public async Task SetIndexReturnsFalseWhenAllDescriptorsDecline(
            LuaCompatibilityVersion version
        )
        {
            CompositeUserDataDescriptor descriptor = CreateComposite(
                new StubDescriptor(indexResult: null, setResult: false),
                new StubDescriptor(indexResult: null, setResult: false)
            );

            bool handled = descriptor.SetIndex(
                new Script(version),
                new object(),
                LuaValue.NewString("k"),
                LuaValue.True,
                true
            );

            await Assert.That(handled).IsFalse();
        }

        [global::TUnit.Core.Test]
        [AllLuaVersions]
        public async Task MetaIndexReturnsFirstNonNullValue(LuaCompatibilityVersion version)
        {
            LuaValue expected = LuaValue.Void;
            StubDescriptor first = new(indexResult: null, metaResult: null);
            StubDescriptor second = new(indexResult: null, metaResult: expected);
            CompositeUserDataDescriptor descriptor = CreateComposite(first, second);

            LuaValue value = descriptor
                .MetaIndex(new Script(version), new object(), "__call")
                .Value;

            await Assert.That(value.IsVoid()).IsTrue();
            await Assert.That(first.MetaCallCount).IsEqualTo(1);
            await Assert.That(second.MetaCallCount).IsEqualTo(1);
        }

        [global::TUnit.Core.Test]
        [AllLuaVersions]
        public async Task MetaIndexReturnsNullWhenNoDescriptorProvidesMeta(
            LuaCompatibilityVersion version
        )
        {
            CompositeUserDataDescriptor descriptor = CreateComposite(
                new StubDescriptor(indexResult: null, metaResult: null),
                new StubDescriptor(indexResult: null, metaResult: null)
            );

            LuaValue? value = descriptor.MetaIndex(new Script(version), new object(), "__add");

            await Assert.That(value).IsNull();
        }

        [global::TUnit.Core.Test]
        [AllLuaVersions]
        public async Task DescriptorsPropertyIsMutable(LuaCompatibilityVersion version)
        {
            CompositeUserDataDescriptor descriptor = CreateComposite();
            LuaValue expected = LuaValue.NewNumber(5);

            descriptor.Descriptors.Add(new StubDescriptor(indexResult: expected));

            LuaValue value = descriptor
                .Index(new Script(version), new object(), LuaValue.NewString("value"), true)
                .Value;

            await Assert.That(value).IsEqualTo(expected);
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
            private readonly LuaValue? _indexResult;
            private readonly bool _setResult;
            private readonly LuaValue? _metaResult;

            public StubDescriptor(
                LuaValue? indexResult,
                bool setResult = false,
                LuaValue? metaResult = default
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

            public bool TryIndex(
                Script script,
                object obj,
                LuaValue index,
                bool isDirectIndexing,
                out LuaValue value
            )
            {
                IndexCallCount++;
                value = _indexResult.GetValueOrDefault();
                return _indexResult.HasValue;
            }

            public bool SetIndex(
                Script script,
                object obj,
                LuaValue index,
                LuaValue value,
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

            public bool TryMetaIndex(Script script, object obj, string metaname, out LuaValue value)
            {
                MetaCallCount++;
                value = _metaResult.GetValueOrDefault();
                return _metaResult.HasValue;
            }

            public bool IsTypeCompatible(Type type, object obj)
            {
                return type.IsInstanceOfType(obj);
            }
        }
    }
}
