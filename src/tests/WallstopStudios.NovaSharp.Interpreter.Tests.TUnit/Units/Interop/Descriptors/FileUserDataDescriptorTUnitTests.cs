namespace WallstopStudios.NovaSharp.Interpreter.Tests.TUnit.Units.Interop.Descriptors
{
    using System;
    using System.Threading.Tasks;
    using global::TUnit.Assertions;
    using global::TUnit.Assertions.Extensions;
    using WallstopStudios.NovaSharp.Interpreter;
    using WallstopStudios.NovaSharp.Interpreter.DataTypes;
    using WallstopStudios.NovaSharp.Interpreter.Errors;
    using WallstopStudios.NovaSharp.Interpreter.Interop;

    /// <summary>
    /// Tests for the <see cref="CoreLib.IO.FileUserDataDescriptor"/> wrapper
    /// that enforces Lua-style indexing semantics for file handles.
    /// </summary>
    public sealed class FileUserDataDescriptorTUnitTests
    {
        [global::TUnit.Core.Test]
        public async Task ConstructorThrowsWhenInnerDescriptorIsNull()
        {
            System.Reflection.TargetInvocationException exception =
                Assert.Throws<System.Reflection.TargetInvocationException>(() =>
                    CreateDescriptor(null!)
                )!;

            await Assert
                .That(exception.InnerException)
                .IsTypeOf<ArgumentNullException>()
                .ConfigureAwait(false);

            await Assert
                .That(exception.InnerException!.Message)
                .Contains("inner")
                .ConfigureAwait(false);
        }

        [global::TUnit.Core.Test]
        public async Task NameDelegatesToInnerDescriptor()
        {
            StubUserDataDescriptor inner = new("TestFile", typeof(object));
            IUserDataDescriptor wrapper = CreateDescriptor(inner);

            await Assert.That(wrapper.Name).IsEqualTo("TestFile").ConfigureAwait(false);
        }

        [global::TUnit.Core.Test]
        public async Task TypeDelegatesToInnerDescriptor()
        {
            StubUserDataDescriptor inner = new("TestFile", typeof(string));
            IUserDataDescriptor wrapper = CreateDescriptor(inner);

            await Assert.That(wrapper.Type).IsEqualTo(typeof(string)).ConfigureAwait(false);
        }

        [global::TUnit.Core.Test]
        public async Task IndexWithNonStringIndexReturnsNil()
        {
            StubUserDataDescriptor inner = new("TestFile", typeof(object));
            IUserDataDescriptor wrapper = CreateDescriptor(inner);
            Script script = new();

            DynValue result = wrapper.Index(
                script,
                new object(),
                DynValue.NewNumber(42),
                isDirectIndexing: true
            );

            await Assert.That(result.Type).IsEqualTo(DataType.Nil).ConfigureAwait(false);
        }

        [global::TUnit.Core.Test]
        public async Task IndexWithStringIndexDelegatesToInner()
        {
            StubUserDataDescriptor inner = new("TestFile", typeof(object))
            {
                IndexResult = DynValue.NewString("method-result"),
            };
            IUserDataDescriptor wrapper = CreateDescriptor(inner);
            Script script = new();

            DynValue result = wrapper.Index(
                script,
                new object(),
                DynValue.NewString("read"),
                isDirectIndexing: true
            );

            await Assert.That(result.Type).IsEqualTo(DataType.String).ConfigureAwait(false);
            await Assert.That(result.String).IsEqualTo("method-result").ConfigureAwait(false);
        }

        [global::TUnit.Core.Test]
        public async Task IndexWithNullIndexDelegatesToInner()
        {
            StubUserDataDescriptor inner = new("TestFile", typeof(object))
            {
                IndexResult = DynValue.NewString("null-index-result"),
            };
            IUserDataDescriptor wrapper = CreateDescriptor(inner);
            Script script = new();

            DynValue result = wrapper.Index(
                script,
                new object(),
                index: null!,
                isDirectIndexing: true
            );

            await Assert.That(result.Type).IsEqualTo(DataType.String).ConfigureAwait(false);
            await Assert.That(result.String).IsEqualTo("null-index-result").ConfigureAwait(false);
        }

        [global::TUnit.Core.Test]
        public async Task SetIndexWithNonStringIndexThrowsIndexTypeError()
        {
            StubUserDataDescriptor inner = new("TestFile", typeof(object));
            IUserDataDescriptor wrapper = CreateDescriptor(inner);
            Script script = new();

            ScriptRuntimeException exception = Assert.Throws<ScriptRuntimeException>(() =>
                wrapper.SetIndex(
                    script,
                    new object(),
                    DynValue.NewNumber(42),
                    DynValue.NewString("value"),
                    isDirectIndexing: true
                )
            )!;

            await Assert.That(exception.Message).Contains("index").ConfigureAwait(false);
        }

        [global::TUnit.Core.Test]
        public async Task SetIndexWithStringIndexDelegatesToInner()
        {
            StubUserDataDescriptor inner = new("TestFile", typeof(object));
            IUserDataDescriptor wrapper = CreateDescriptor(inner);
            Script script = new();

            bool result = wrapper.SetIndex(
                script,
                new object(),
                DynValue.NewString("field"),
                DynValue.NewString("value"),
                isDirectIndexing: true
            );

            await Assert.That(result).IsTrue().ConfigureAwait(false);
            await Assert.That(inner.SetIndexCallCount).IsEqualTo(1).ConfigureAwait(false);
        }

        [global::TUnit.Core.Test]
        public async Task SetIndexWithNullIndexDelegatesToInner()
        {
            StubUserDataDescriptor inner = new("TestFile", typeof(object));
            IUserDataDescriptor wrapper = CreateDescriptor(inner);
            Script script = new();

            bool result = wrapper.SetIndex(
                script,
                new object(),
                index: null!,
                DynValue.NewString("value"),
                isDirectIndexing: true
            );

            await Assert.That(result).IsTrue().ConfigureAwait(false);
            await Assert.That(inner.SetIndexCallCount).IsEqualTo(1).ConfigureAwait(false);
        }

        [global::TUnit.Core.Test]
        public async Task AsStringDelegatesToInnerDescriptor()
        {
            StubUserDataDescriptor inner = new("TestFile", typeof(object))
            {
                AsStringResult = "file: 0x12345",
            };
            IUserDataDescriptor wrapper = CreateDescriptor(inner);

            string result = wrapper.AsString(new object());

            await Assert.That(result).IsEqualTo("file: 0x12345").ConfigureAwait(false);
        }

        [global::TUnit.Core.Test]
        public async Task MetaIndexDelegatesToInnerDescriptor()
        {
            StubUserDataDescriptor inner = new("TestFile", typeof(object))
            {
                MetaIndexResult = DynValue.NewString("__gc"),
            };
            IUserDataDescriptor wrapper = CreateDescriptor(inner);
            Script script = new();

            DynValue result = wrapper.MetaIndex(script, new object(), "__gc");

            await Assert.That(result.Type).IsEqualTo(DataType.String).ConfigureAwait(false);
            await Assert.That(result.String).IsEqualTo("__gc").ConfigureAwait(false);
        }

        [global::TUnit.Core.Test]
        public async Task IsTypeCompatibleDelegatesToInnerDescriptor()
        {
            StubUserDataDescriptor inner = new("TestFile", typeof(object))
            {
                IsTypeCompatibleResult = true,
            };
            IUserDataDescriptor wrapper = CreateDescriptor(inner);

            bool result = wrapper.IsTypeCompatible(typeof(string), new object());

            await Assert.That(result).IsTrue().ConfigureAwait(false);
            await Assert.That(inner.IsTypeCompatibleCallCount).IsEqualTo(1).ConfigureAwait(false);
        }

        [global::TUnit.Core.Test]
        public async Task IsTypeCompatibleReturnsFalseWhenInnerReturnsFalse()
        {
            StubUserDataDescriptor inner = new("TestFile", typeof(object))
            {
                IsTypeCompatibleResult = false,
            };
            IUserDataDescriptor wrapper = CreateDescriptor(inner);

            bool result = wrapper.IsTypeCompatible(typeof(int), new object());

            await Assert.That(result).IsFalse().ConfigureAwait(false);
        }

        /// <summary>
        /// Creates a FileUserDataDescriptor by reflection since the type is internal.
        /// </summary>
        private static IUserDataDescriptor CreateDescriptor(IUserDataDescriptor inner)
        {
            Type fileUserDataDescriptorType = typeof(Script).Assembly.GetType(
                "WallstopStudios.NovaSharp.Interpreter.CoreLib.IO.FileUserDataDescriptor",
                throwOnError: true
            )!;

            return (IUserDataDescriptor)
                Activator.CreateInstance(
                    fileUserDataDescriptorType,
                    System.Reflection.BindingFlags.Instance
                        | System.Reflection.BindingFlags.NonPublic,
                    binder: null,
                    args: new object[] { inner! },
                    culture: null
                )!;
        }

        private sealed class StubUserDataDescriptor : IUserDataDescriptor
        {
            public StubUserDataDescriptor(string name, Type type)
            {
                Name = name;
                Type = type;
            }

            public string Name { get; }

            public Type Type { get; }

            public DynValue IndexResult { get; set; } = DynValue.Nil;

            public string AsStringResult { get; set; } = "stub";

            public DynValue MetaIndexResult { get; set; } = DynValue.Nil;

            public bool IsTypeCompatibleResult { get; set; }

            public int SetIndexCallCount { get; private set; }

            public int IsTypeCompatibleCallCount { get; private set; }

            public DynValue Index(Script script, object obj, DynValue index, bool isDirectIndexing)
            {
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
                return true;
            }

            public string AsString(object obj)
            {
                return AsStringResult;
            }

            public DynValue MetaIndex(Script script, object obj, string metaname)
            {
                return MetaIndexResult;
            }

            public bool IsTypeCompatible(Type type, object obj)
            {
                IsTypeCompatibleCallCount++;
                return IsTypeCompatibleResult;
            }
        }
    }
}
