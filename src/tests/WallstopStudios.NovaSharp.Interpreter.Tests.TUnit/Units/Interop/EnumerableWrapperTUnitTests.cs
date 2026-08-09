namespace WallstopStudios.NovaSharp.Interpreter.Tests.TUnit.Units.Interop
{
    using System.Collections;
    using System.Collections.Generic;
    using System.Threading.Tasks;
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

            DynValue tuple = EnumerableWrapper.ConvertIterator(script, enumerator);
            DynValue iteratorUserData = tuple.Tuple[0];
            DynValue iteratorCallback = GetIteratorCallback(script, iteratorUserData);
            ScriptExecutionContext context = TestHelpers.CreateExecutionContext(script);

            DynValue first = iteratorCallback.Callback.ClrCallback(
                context,
                TestHelpers.CreateArguments()
            );
            DynValue second = iteratorCallback.Callback.ClrCallback(
                context,
                TestHelpers.CreateArguments()
            );
            DynValue third = iteratorCallback.Callback.ClrCallback(
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
            await Assert.That(third.IsNil()).IsTrue().ConfigureAwait(false);
        }

        [global::TUnit.Core.Test]
        public async Task IteratorSkipsNilValuesAndResetsOnNextCycle()
        {
            Script script = new();
            TrackingEnumerator enumerator = new(5, null, 7);
            DynValue iteratorUserData = EnumerableWrapper.ConvertIterator(script, enumerator).Tuple[
                0
            ];
            DynValue iteratorCallback = GetIteratorCallback(script, iteratorUserData);
            ScriptExecutionContext context = TestHelpers.CreateExecutionContext(script);

            DynValue first = iteratorCallback.Callback.ClrCallback(
                context,
                TestHelpers.CreateArguments()
            );
            DynValue second = iteratorCallback.Callback.ClrCallback(
                context,
                TestHelpers.CreateArguments()
            );
            DynValue third = iteratorCallback.Callback.ClrCallback(
                context,
                TestHelpers.CreateArguments()
            );

            await Assert.That(first.Number).IsEqualTo(5).ConfigureAwait(false);
            await Assert.That(second.Number).IsEqualTo(7).ConfigureAwait(false);
            await Assert.That(third.IsNil()).IsTrue().ConfigureAwait(false);
            await Assert.That(enumerator.ResetCalls).IsZero().ConfigureAwait(false);

            DynValue restart = iteratorCallback.Callback.ClrCallback(
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
            DynValue iteratorUserData = EnumerableWrapper.ConvertIterator(script, enumerator).Tuple[
                0
            ];
            (IUserDataDescriptor descriptor, object instance) = GetDescriptor(iteratorUserData);
            ScriptExecutionContext context = TestHelpers.CreateExecutionContext(script);

            DynValue moveNext = RequireIndex(
                descriptor,
                script,
                instance,
                DynValue.NewString("MoveNext"),
                isDirectIndexing: true
            );
            bool advanced = moveNext
                .Callback.ClrCallback(context, TestHelpers.CreateArguments())
                .Boolean;

            await Assert.That(advanced).IsTrue().ConfigureAwait(false);

            DynValue current = RequireIndex(
                descriptor,
                script,
                instance,
                DynValue.NewString("Current"),
                isDirectIndexing: true
            );
            await Assert.That(current.String).IsEqualTo("alpha").ConfigureAwait(false);

            DynValue resetCallback = RequireIndex(
                descriptor,
                script,
                instance,
                DynValue.NewString("Reset"),
                isDirectIndexing: true
            );
            DynValue resetResult = resetCallback.Callback.ClrCallback(
                context,
                TestHelpers.CreateArguments()
            );
            await Assert.That(resetResult.IsNil()).IsTrue().ConfigureAwait(false);

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
            table.Append(DynValue.NewNumber(10));
            table.Append(DynValue.NewNumber(20));

            DynValue iteratorUserData = EnumerableWrapper.ConvertTable(table).Tuple[0];
            DynValue iteratorCallback = GetIteratorCallback(script, iteratorUserData);
            ScriptExecutionContext context = TestHelpers.CreateExecutionContext(script);

            DynValue first = iteratorCallback.Callback.ClrCallback(
                context,
                TestHelpers.CreateArguments()
            );
            DynValue second = iteratorCallback.Callback.ClrCallback(
                context,
                TestHelpers.CreateArguments()
            );
            DynValue third = iteratorCallback.Callback.ClrCallback(
                context,
                TestHelpers.CreateArguments()
            );

            await Assert.That(first.Number).IsEqualTo(10).ConfigureAwait(false);
            await Assert.That(second.Number).IsEqualTo(20).ConfigureAwait(false);
            await Assert.That(third.IsNil()).IsTrue().ConfigureAwait(false);
        }

        [global::TUnit.Core.Test]
        public async Task IndexRecognizesAlternateNamesAndIgnoresUnknownEntries()
        {
            Script script = new();
            TrackingEnumerator enumerator = new("one", "two");
            DynValue iteratorUserData = EnumerableWrapper.ConvertIterator(script, enumerator).Tuple[
                0
            ];
            (IUserDataDescriptor descriptor, object instance) = GetDescriptor(iteratorUserData);
            ScriptExecutionContext context = TestHelpers.CreateExecutionContext(script);

            DynValue moveNext = RequireIndex(
                descriptor,
                script,
                instance,
                DynValue.NewString("move_next"),
                true
            );
            DynValue reset = RequireIndex(
                descriptor,
                script,
                instance,
                DynValue.NewString("reset"),
                true
            );

            DynValue GetCurrentAccessor()
            {
                return RequireIndex(
                    descriptor,
                    script,
                    instance,
                    DynValue.NewString("current"),
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
                DynValue.NewString("does_not_exist"),
                true,
                out DynValue missing
            );
            await Assert.That(foundUnknown).IsFalse().ConfigureAwait(false);
            await Assert.That(missing.IsNil()).IsTrue().ConfigureAwait(false);

            DynValue resetResult = reset.Callback.ClrCallback(
                context,
                TestHelpers.CreateArguments()
            );
            await Assert.That(resetResult.IsNil()).IsTrue().ConfigureAwait(false);

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
            DynValue iteratorUserData = EnumerableWrapper.ConvertIterator(script, enumerator).Tuple[
                0
            ];
            (IUserDataDescriptor descriptor, object instance) = GetDescriptor(iteratorUserData);

            bool result = descriptor.SetIndex(
                script,
                instance,
                DynValue.NewString("any"),
                DynValue.NewNumber(1),
                isDirectIndexing: true
            );

            await Assert.That(result).IsFalse().ConfigureAwait(false);
        }

        [global::TUnit.Core.Test]
        public async Task MetaIndexReturnsNullForUnsupportedNames()
        {
            Script script = new();
            TrackingEnumerator enumerator = new(1);
            DynValue iteratorUserData = EnumerableWrapper.ConvertIterator(script, enumerator).Tuple[
                0
            ];
            (IUserDataDescriptor descriptor, object instance) = GetDescriptor(iteratorUserData);

            bool found = descriptor.TryMetaIndex(script, instance, "__len", out DynValue value);

            await Assert.That(found).IsFalse().ConfigureAwait(false);
            await Assert.That(value.IsNil()).IsTrue().ConfigureAwait(false);
        }

        private static DynValue GetIteratorCallback(Script script, DynValue iteratorUserData)
        {
            (IUserDataDescriptor descriptor, object instance) = GetDescriptor(iteratorUserData);
            return descriptor.TryMetaIndex(script, instance, "__call", out DynValue callback)
                ? callback
                : throw new global::System.InvalidOperationException(
                    "iterator callback should exist"
                );
        }

        private static DynValue RequireIndex(
            IUserDataDescriptor descriptor,
            Script script,
            object instance,
            DynValue index,
            bool isDirectIndexing
        )
        {
            return descriptor.TryIndex(
                script,
                instance,
                index,
                isDirectIndexing,
                out DynValue value
            )
                ? value
                : throw new global::System.InvalidOperationException(
                    $"{index.ToPrintString()} should exist"
                );
        }

        private static (IUserDataDescriptor descriptor, object instance) GetDescriptor(
            DynValue iteratorUserData
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
