namespace NovaSharp.Interpreter.Tests.Units
{
    using System.Collections;
    using System.Collections.Generic;
    using NovaSharp.Interpreter;
    using NovaSharp.Interpreter.DataTypes;
    using NovaSharp.Interpreter.Execution;
    using NovaSharp.Interpreter.Interop;
    using NovaSharp.Interpreter.Interop.PredefinedUserData;
    using NUnit.Framework;

    [TestFixture]
    public sealed class EnumerableWrapperTests
    {
        [Test]
        public void ConvertIteratorExposesCallableUserData()
        {
            Script script = new Script();
            TrackingEnumerator enumerator = new TrackingEnumerator(1, 2);

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

            Assert.Multiple(() =>
            {
                Assert.That(tuple.Tuple.Length, Is.EqualTo(3));
                Assert.That(iteratorUserData.Type, Is.EqualTo(DataType.UserData));
                Assert.That(first.Number, Is.EqualTo(1));
                Assert.That(second.Number, Is.EqualTo(2));
                Assert.That(third.IsNil(), Is.True);
            });
        }

        [Test]
        public void IteratorSkipsNilValuesAndResetsOnNextCycle()
        {
            Script script = new Script();
            TrackingEnumerator enumerator = new TrackingEnumerator(5, null, 7);
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

            Assert.Multiple(() =>
            {
                Assert.That(first.Number, Is.EqualTo(5));
                Assert.That(second.Number, Is.EqualTo(7), "null entries should be skipped");
                Assert.That(third.IsNil(), Is.True);
                Assert.That(enumerator.ResetCalls, Is.Zero);
            });

            DynValue restart = iteratorCallback.Callback.ClrCallback(
                context,
                TestHelpers.CreateArguments()
            );
            Assert.Multiple(() =>
            {
                Assert.That(
                    enumerator.ResetCalls,
                    Is.EqualTo(1),
                    "Reset should run before the next cycle"
                );
                Assert.That(restart.Number, Is.EqualTo(5));
            });
        }

        [Test]
        public void IndexProvidesCurrentMoveNextAndResetCallbacks()
        {
            Script script = new Script();
            TrackingEnumerator enumerator = new TrackingEnumerator("alpha", "beta");
            DynValue iteratorUserData = EnumerableWrapper.ConvertIterator(script, enumerator).Tuple[
                0
            ];
            (IUserDataDescriptor descriptor, object instance) = GetDescriptor(iteratorUserData);
            ScriptExecutionContext context = TestHelpers.CreateExecutionContext(script);

            DynValue moveNext = descriptor.Index(
                script,
                instance,
                DynValue.NewString("MoveNext"),
                isDirectIndexing: true
            );
            bool advanced = moveNext
                .Callback.ClrCallback(context, TestHelpers.CreateArguments())
                .Boolean;
            Assert.That(advanced, Is.True);

            DynValue current = descriptor.Index(
                script,
                instance,
                DynValue.NewString("Current"),
                isDirectIndexing: true
            );
            Assert.That(current.String, Is.EqualTo("alpha"));

            DynValue resetCallback = descriptor.Index(
                script,
                instance,
                DynValue.NewString("Reset"),
                isDirectIndexing: true
            );
            DynValue resetResult = resetCallback.Callback.ClrCallback(
                context,
                TestHelpers.CreateArguments()
            );
            Assert.That(resetResult.IsNil(), Is.True);

            bool restarted = moveNext
                .Callback.ClrCallback(context, TestHelpers.CreateArguments())
                .Boolean;
            Assert.That(restarted, Is.True);
            Assert.That(current.String, Is.EqualTo("alpha"), "Reset should rewind the enumerator");
        }

        [Test]
        public void ConvertTableIteratesOverTableValues()
        {
            Script script = new Script();
            Table table = new Table(script);
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

            Assert.Multiple(() =>
            {
                Assert.That(first.Number, Is.EqualTo(10));
                Assert.That(second.Number, Is.EqualTo(20));
                Assert.That(third.IsNil(), Is.True);
            });
        }

        private static DynValue GetIteratorCallback(Script script, DynValue iteratorUserData)
        {
            (IUserDataDescriptor descriptor, object instance) = GetDescriptor(iteratorUserData);
            return descriptor.MetaIndex(script, instance, "__call");
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
