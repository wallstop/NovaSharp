namespace NovaSharp.Interpreter.Tests.Units
{
    using System;
    using System.Collections;
    using System.Collections.Generic;
    using NovaSharp.Interpreter;
    using NovaSharp.Interpreter.DataTypes;
    using NovaSharp.Interpreter.Debugging;
    using NovaSharp.Interpreter.Execution;
    using NUnit.Framework;

    [TestFixture]
    public sealed class CoroutineApiTests
    {
        [Test]
        public void AsTypedEnumerableReturnsAllResults()
        {
            Script script = new();
            DynValue function = script.DoString(
                @"
                return function()
                    coroutine.yield(1)
                    return 2
                end
                "
            );

            Coroutine coroutine = script.CreateCoroutine(function).Coroutine;

            List<double> results = new();
            foreach (DynValue value in coroutine.AsTypedEnumerable())
            {
                results.Add(value.ToScalar().ToObject<double>());
            }

            Assert.Multiple(() =>
            {
                Assert.That(results, Is.EqualTo(new[] { 1d, 2d }));
                Assert.That(coroutine.State, Is.EqualTo(CoroutineState.Dead));
            });
        }

        [Test]
        public void AsTypedEnumerableThrowsForClrCallback()
        {
            CallbackFunction callback = DynValue
                .NewCallback((_, _) => DynValue.NewNumber(1))
                .Callback;
            Coroutine coroutine = new(callback);

            IEnumerator<DynValue> enumerator = coroutine.AsTypedEnumerable().GetEnumerator();

            Assert.That(
                () => enumerator.MoveNext(),
                Throws
                    .TypeOf<InvalidOperationException>()
                    .With.Message.Contains("Only non-CLR coroutines can be resumed")
            );
        }

        [Test]
        public void AsEnumerableReturnsObjects()
        {
            Script script = new();
            DynValue function = script.DoString(
                @"
                return function()
                    coroutine.yield('foo')
                    return 7
                end
                "
            );

            Coroutine coroutine = script.CreateCoroutine(function).Coroutine;
            List<object> results = new();

            foreach (object value in coroutine.AsEnumerable())
            {
                results.Add(value);
            }

            Assert.That(results, Is.EqualTo(new object[] { "foo", 7d }));
        }

        [Test]
        public void AsEnumerableOfTPerformsConversion()
        {
            Script script = new();
            DynValue function = script.DoString(
                @"
                return function()
                    coroutine.yield(10)
                    return 20
                end
                "
            );

            Coroutine coroutine = script.CreateCoroutine(function).Coroutine;
            List<int> results = new();

            foreach (int value in coroutine.AsEnumerable<int>())
            {
                results.Add(value);
            }

            Assert.That(results, Is.EqualTo(new[] { 10, 20 }));
        }

        [Test]
        public void AsUnityCoroutineYieldsNullAndCompletes()
        {
            Script script = new();
            DynValue function = script.DoString(
                @"
                return function()
                    coroutine.yield('pause-1')
                    coroutine.yield('pause-2')
                    return 'done'
                end
                "
            );

            Coroutine coroutine = script.CreateCoroutine(function).Coroutine;
            IEnumerator enumerator = coroutine.AsUnityCoroutine();

            List<object> yielded = new();
            while (enumerator.MoveNext())
            {
                yielded.Add(enumerator.Current);
            }

            Assert.That(yielded, Is.EqualTo(new object[] { null, null, null }));
            Assert.That(coroutine.State, Is.EqualTo(CoroutineState.Dead));
        }

        [Test]
        public void ResumeWithObjectParamsConvertsArguments()
        {
            Script script = new();
            DynValue function = script.DoString(
                @"
                return function(a, b)
                    return a + b
                end
                "
            );

            Coroutine coroutine = script.CreateCoroutine(function).Coroutine;
            DynValue result = coroutine.Resume(10, 32);

            Assert.Multiple(() =>
            {
                Assert.That(result.Type, Is.EqualTo(DataType.Number));
                Assert.That(result.Number, Is.EqualTo(42d));
                Assert.That(coroutine.State, Is.EqualTo(CoroutineState.Dead));
            });
        }

        [Test]
        public void ResumeWithContextAndObjectParamsConvertsArguments()
        {
            Script script = new();
            DynValue function = script.DoString(
                @"
                return function(value)
                    return value
                end
                "
            );

            Coroutine coroutine = script.CreateCoroutine(function).Coroutine;
            ScriptExecutionContext context = TestHelpers.CreateExecutionContext(script);

            DynValue result = coroutine.Resume(context, "hello");

            Assert.That(result.ToScalar().ToObject<string>(), Is.EqualTo("hello"));
        }

        [Test]
        public void MarkClrCallbackTransitionsState()
        {
            CallbackFunction callback = DynValue
                .NewCallback((_, _) => DynValue.NewNumber(1))
                .Callback;
            Coroutine coroutine = new(callback);

            Assert.Multiple(() =>
            {
                Assert.That(coroutine.Type, Is.EqualTo(Coroutine.CoroutineType.ClrCallback));
                Assert.That(coroutine.State, Is.EqualTo(CoroutineState.NotStarted));
            });

            coroutine.MarkClrCallbackAsDead();

            Assert.Multiple(() =>
            {
                Assert.That(coroutine.Type, Is.EqualTo(Coroutine.CoroutineType.ClrCallbackDead));
                Assert.That(coroutine.State, Is.EqualTo(CoroutineState.Dead));
            });
        }

        [Test]
        public void MarkClrCallbackThrowsWhenNotClr()
        {
            Script script = new();
            DynValue function = script.DoString("return function() return 0 end");
            Coroutine coroutine = script.CreateCoroutine(function).Coroutine;

            Assert.That(
                () => coroutine.MarkClrCallbackAsDead(),
                Throws
                    .TypeOf<InvalidOperationException>()
                    .With.Message.Contains("CoroutineType.ClrCallback")
            );
        }

        [Test]
        public void ResumeWithoutContextThrowsForClrCallback()
        {
            CallbackFunction callback = DynValue
                .NewCallback((_, _) => DynValue.NewNumber(1))
                .Callback;
            Coroutine coroutine = new(callback);

            Assert.That(
                () => coroutine.Resume(),
                Throws
                    .TypeOf<InvalidOperationException>()
                    .With.Message.Contains("Only non-CLR coroutines")
            );
        }

        [Test]
        public void GetStackTraceReturnsSuspendedFrames()
        {
            Script script = new();
            script.Options.DebugPrint = _ => { };
            DynValue function = script.DoString(
                @"
                return function()
                    local function inner()
                        coroutine.yield('pause')
                    end
                    inner()
                    return 'done'
                end
                "
            );

            Coroutine coroutine = script.CreateCoroutine(function).Coroutine;
            DynValue yielded = coroutine.Resume();
            Assert.That(yielded.ToScalar().ToObject<string>(), Is.EqualTo("pause"));
            Assert.That(coroutine.State, Is.EqualTo(CoroutineState.Suspended));

            WatchItem[] stack = coroutine.GetStackTrace(0);

            Assert.That(stack.Length, Is.GreaterThan(0));
        }

        [Test]
        public void AutoYieldCounterCanBeReadAndWritten()
        {
            Script script = new();
            DynValue function = script.DoString(
                @"
                return function()
                    return 0
                end
                "
            );

            Coroutine coroutine = script.CreateCoroutine(function).Coroutine;
            coroutine.AutoYieldCounter = 5;

            Assert.That(coroutine.AutoYieldCounter, Is.EqualTo(5));
        }
    }
}
