namespace WallstopStudios.NovaSharp.Interpreter.Tests.TUnit.Units.Interop
{
    using System.Collections;
    using System.Collections.Generic;
    using System.Threading.Tasks;
    using global::NovaSharp;
    using global::TUnit.Assertions;
    using WallstopStudios.NovaSharp.Interpreter;
    using WallstopStudios.NovaSharp.Interpreter.DataTypes;
    using WallstopStudios.NovaSharp.Interpreter.Execution;
    using WallstopStudios.NovaSharp.Interpreter.Interop;
    using WallstopStudios.NovaSharp.Interpreter.Interop.PredefinedUserData;
    using WallstopStudios.NovaSharp.Interpreter.Tests.Units;

    public sealed class EnumerableWrapperTUnitTests
    {
        [global::TUnit.Core.Test]
        public async Task ConvertIteratorExposesCallableUserData()
        {
            Script script = new();
            TrackingEnumerator enumerator = new(1, 2);

            LuaValue tuple = EnumerableWrapper.ConvertIterator(script, enumerator);
            LuaValue iteratorUserData = tuple.Tuple[0];
            LuaValue iteratorCallback = GetIteratorCallback(script, iteratorUserData);
            ScriptExecutionContext context = TestHelpers.CreateExecutionContext(script);

            LuaValue first = iteratorCallback.Callback.ClrCallback(
                context,
                TestHelpers.CreateArguments()
            );
            LuaValue second = iteratorCallback.Callback.ClrCallback(
                context,
                TestHelpers.CreateArguments()
            );
            LuaValue third = iteratorCallback.Callback.ClrCallback(
                context,
                TestHelpers.CreateArguments()
            );

            await Assert.That(tuple.Tuple.Length).IsEqualTo(3).ConfigureAwait(false);
            await Assert
                .That(iteratorUserData.Type)
                .IsEqualTo(DataType.UserData)
                .ConfigureAwait(false);
            await Assert.That(first.Number).IsEqualTo(1).ConfigureAwait(false);
            await Assert.That(second.Number).IsEqualTo(2).ConfigureAwait(false);
            await Assert.That(third.IsNil).IsTrue().ConfigureAwait(false);
        }

        [global::TUnit.Core.Test]
        public async Task IteratorSkipsNilValuesAndResetsOnNextCycle()
        {
            Script script = new();
            TrackingEnumerator enumerator = new(5, null, 7);
            LuaValue iteratorUserData = EnumerableWrapper.ConvertIterator(script, enumerator).Tuple[
                0
            ];
            LuaValue iteratorCallback = GetIteratorCallback(script, iteratorUserData);
            ScriptExecutionContext context = TestHelpers.CreateExecutionContext(script);

            LuaValue first = iteratorCallback.Callback.ClrCallback(
                context,
                TestHelpers.CreateArguments()
            );
            LuaValue second = iteratorCallback.Callback.ClrCallback(
                context,
                TestHelpers.CreateArguments()
            );
            LuaValue third = iteratorCallback.Callback.ClrCallback(
                context,
                TestHelpers.CreateArguments()
            );

            await Assert.That(first.Number).IsEqualTo(5).ConfigureAwait(false);
            await Assert.That(second.Number).IsEqualTo(7).ConfigureAwait(false);
            await Assert.That(third.IsNil).IsTrue().ConfigureAwait(false);
            await Assert.That(enumerator.ResetCalls).IsZero().ConfigureAwait(false);

            LuaValue restart = iteratorCallback.Callback.ClrCallback(
                context,
                TestHelpers.CreateArguments()
            );

            await Assert.That(enumerator.ResetCalls).IsEqualTo(1).ConfigureAwait(false);
            await Assert.That(restart.Number).IsEqualTo(5).ConfigureAwait(false);
        }

        [global::TUnit.Core.Test]
        public async Task IndexProvidesCurrentMoveNextAndResetCallbacks()
        {
            Script script = new();
            TrackingEnumerator enumerator = new("alpha", "beta");
            LuaValue iteratorUserData = EnumerableWrapper.ConvertIterator(script, enumerator).Tuple[
                0
            ];
            (IUserDataDescriptor descriptor, object instance) = GetDescriptor(iteratorUserData);
            ScriptExecutionContext context = TestHelpers.CreateExecutionContext(script);

            LuaValue moveNext = RequireIndex(
                descriptor,
                script,
                instance,
                LuaValue.NewString("MoveNext"),
                isDirectIndexing: true
            );
            bool advanced = moveNext
                .Callback.ClrCallback(context, TestHelpers.CreateArguments())
                .Boolean;

            await Assert.That(advanced).IsTrue().ConfigureAwait(false);

            LuaValue current = RequireIndex(
                descriptor,
                script,
                instance,
                LuaValue.NewString("Current"),
                isDirectIndexing: true
            );
            await Assert.That(current.String).IsEqualTo("alpha").ConfigureAwait(false);

            LuaValue resetCallback = RequireIndex(
                descriptor,
                script,
                instance,
                LuaValue.NewString("Reset"),
                isDirectIndexing: true
            );
            LuaValue resetResult = resetCallback.Callback.ClrCallback(
                context,
                TestHelpers.CreateArguments()
            );
            await Assert.That(resetResult.IsNil).IsTrue().ConfigureAwait(false);

            bool restarted = moveNext
                .Callback.ClrCallback(context, TestHelpers.CreateArguments())
                .Boolean;

            await Assert.That(restarted).IsTrue().ConfigureAwait(false);
            await Assert.That(current.String).IsEqualTo("alpha").ConfigureAwait(false);
        }

        [global::TUnit.Core.Test]
        public async Task ConvertTableIteratesOverTableValues()
        {
            Script script = new();
            Table table = new(script);
            table.Append(LuaValue.NewNumber(10));
            table.Append(LuaValue.NewNumber(20));

            LuaValue iteratorUserData = EnumerableWrapper.ConvertTable(table).Tuple[0];
            LuaValue iteratorCallback = GetIteratorCallback(script, iteratorUserData);
            ScriptExecutionContext context = TestHelpers.CreateExecutionContext(script);

            LuaValue first = iteratorCallback.Callback.ClrCallback(
                context,
                TestHelpers.CreateArguments()
            );
            LuaValue second = iteratorCallback.Callback.ClrCallback(
                context,
                TestHelpers.CreateArguments()
            );
            LuaValue third = iteratorCallback.Callback.ClrCallback(
                context,
                TestHelpers.CreateArguments()
            );

            await Assert.That(first.Number).IsEqualTo(10).ConfigureAwait(false);
            await Assert.That(second.Number).IsEqualTo(20).ConfigureAwait(false);
            await Assert.That(third.IsNil).IsTrue().ConfigureAwait(false);
        }

        [global::TUnit.Core.Test]
        public async Task IndexRecognizesAlternateNamesAndIgnoresUnknownEntries()
        {
            Script script = new();
            TrackingEnumerator enumerator = new("one", "two");
            LuaValue iteratorUserData = EnumerableWrapper.ConvertIterator(script, enumerator).Tuple[
                0
            ];
            (IUserDataDescriptor descriptor, object instance) = GetDescriptor(iteratorUserData);
            ScriptExecutionContext context = TestHelpers.CreateExecutionContext(script);

            LuaValue moveNext = RequireIndex(
                descriptor,
                script,
                instance,
                LuaValue.NewString("move_next"),
                true
            );
            LuaValue reset = RequireIndex(
                descriptor,
                script,
                instance,
                LuaValue.NewString("reset"),
                true
            );

            LuaValue GetCurrentAccessor()
            {
                return RequireIndex(
                    descriptor,
                    script,
                    instance,
                    LuaValue.NewString("current"),
                    true
                );
            }

            await Assert
                .That(moveNext.Callback.ClrCallback(context, TestHelpers.CreateArguments()).Boolean)
                .IsTrue()
                .ConfigureAwait(false);
            await Assert.That(GetCurrentAccessor().String).IsEqualTo("one").ConfigureAwait(false);
            await Assert
                .That(moveNext.Callback.ClrCallback(context, TestHelpers.CreateArguments()).Boolean)
                .IsTrue()
                .ConfigureAwait(false);
            await Assert.That(GetCurrentAccessor().String).IsEqualTo("two").ConfigureAwait(false);

            bool foundUnknown = descriptor.TryIndex(
                script,
                instance,
                LuaValue.NewString("does_not_exist"),
                true,
                out LuaValue missing
            );
            await Assert.That(foundUnknown).IsFalse().ConfigureAwait(false);
            await Assert.That(missing.IsNil).IsTrue().ConfigureAwait(false);

            LuaValue resetResult = reset.Callback.ClrCallback(
                context,
                TestHelpers.CreateArguments()
            );
            await Assert.That(resetResult.IsNil).IsTrue().ConfigureAwait(false);

            reset.Callback.ClrCallback(context, TestHelpers.CreateArguments());

            bool restarted = moveNext
                .Callback.ClrCallback(context, TestHelpers.CreateArguments())
                .Boolean;

            await Assert.That(restarted).IsTrue().ConfigureAwait(false);
            await Assert.That(GetCurrentAccessor().String).IsEqualTo("one").ConfigureAwait(false);
        }

        [global::TUnit.Core.Test]
        public async Task SetIndexAlwaysReturnsFalse()
        {
            Script script = new();
            TrackingEnumerator enumerator = new();
            LuaValue iteratorUserData = EnumerableWrapper.ConvertIterator(script, enumerator).Tuple[
                0
            ];
            (IUserDataDescriptor descriptor, object instance) = GetDescriptor(iteratorUserData);

            bool result = descriptor.SetIndex(
                script,
                instance,
                LuaValue.NewString("any"),
                LuaValue.NewNumber(1),
                isDirectIndexing: true
            );

            await Assert.That(result).IsFalse().ConfigureAwait(false);
        }

        [global::TUnit.Core.Test]
        public async Task MetaIndexReturnsNullForUnsupportedNames()
        {
            Script script = new();
            TrackingEnumerator enumerator = new(1);
            LuaValue iteratorUserData = EnumerableWrapper.ConvertIterator(script, enumerator).Tuple[
                0
            ];
            (IUserDataDescriptor descriptor, object instance) = GetDescriptor(iteratorUserData);

            bool found = descriptor.TryMetaIndex(script, instance, "__len", out LuaValue value);

            await Assert.That(found).IsFalse().ConfigureAwait(false);
            await Assert.That(value.IsNil).IsTrue().ConfigureAwait(false);
        }

        private static LuaValue GetIteratorCallback(Script script, LuaValue iteratorUserData)
        {
            (IUserDataDescriptor descriptor, object instance) = GetDescriptor(iteratorUserData);
            return descriptor.TryMetaIndex(script, instance, "__call", out LuaValue callback)
                ? callback
                : throw new global::System.InvalidOperationException(
                    "iterator callback should exist"
                );
        }

        private static LuaValue RequireIndex(
            IUserDataDescriptor descriptor,
            Script script,
            object instance,
            LuaValue index,
            bool isDirectIndexing
        )
        {
            return descriptor.TryIndex(
                script,
                instance,
                index,
                isDirectIndexing,
                out LuaValue value
            )
                ? value
                : throw new global::System.InvalidOperationException(
                    $"{index.ToPrintString()} should exist"
                );
        }

        private static (IUserDataDescriptor descriptor, object instance) GetDescriptor(
            LuaValue iteratorUserData
        )
        {
            UserData userData = iteratorUserData.UserData;
            return (userData.Descriptor, userData.Object);
        }

        private sealed class TrackingEnumerator : IEnumerator
        {
            private readonly object[] _items;
            private int _position = -1;

            internal TrackingEnumerator(params object[] items)
            {
                _items = items;
            }

            internal int ResetCalls { get; private set; }

            public object Current => _items[_position];

            public bool MoveNext()
            {
                _position++;
                return _position < _items.Length;
            }

            public void Reset()
            {
                ResetCalls++;
                _position = -1;
            }
        }
    }
}
