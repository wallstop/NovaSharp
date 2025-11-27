namespace NovaSharp.Interpreter.Tests.Units
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using NovaSharp.Interpreter;
    using NovaSharp.Interpreter.DataTypes;
    using NovaSharp.Interpreter.Errors;
    using NovaSharp.Interpreter.Execution;
    using NovaSharp.Interpreter.Execution.VM;
    using NUnit.Framework;

    [TestFixture]
    public sealed class CoroutineTests
    {
        private static readonly int[] YieldedValues = { 1, 2, 3 };

        [Test]
        public void ResumeAfterCompletionThrows()
        {
            Script script = new();
            DynValue function = script.DoString("return function() return 1 end");
            DynValue coroutine = script.CreateCoroutine(function);

            DynValue first = coroutine.Coroutine.Resume();
            Assert.That(first.Number, Is.EqualTo(1));

            ScriptRuntimeException exception = Assert.Throws<ScriptRuntimeException>(() =>
                coroutine.Coroutine.Resume()
            );

            Assert.That(exception.Message, Does.Contain("cannot resume dead coroutine"));
        }

        [Test]
        public void AsTypedEnumerableIteratesAllResults()
        {
            Script script = new();
            DynValue function = script.DoString(
                "return function() coroutine.yield(1) coroutine.yield(2) return 3 end"
            );
            DynValue coroutine = script.CreateCoroutine(function);

            List<int> results = new();
            foreach (DynValue value in coroutine.Coroutine.AsTypedEnumerable())
            {
                results.Add((int)value.Number);
            }

            Assert.That(results, Is.EqualTo(YieldedValues));
        }

        [Test]
        public void AsTypedEnumerableThrowsForClrCallbacks()
        {
            Script script = new();
            DynValue callback = DynValue.NewCallback((_, _) => DynValue.Nil);
            DynValue coroutine = script.CreateCoroutine(callback);

            InvalidOperationException exception = Assert.Throws<InvalidOperationException>(() =>
                coroutine.Coroutine.AsTypedEnumerable().ToList()
            );

            Assert.That(exception.Message, Does.Contain("Only non-CLR coroutines"));
        }

        [Test]
        public void AsEnumerableReturnsObjects()
        {
            Script script = new();
            DynValue function = script.DoString(
                "return function() coroutine.yield(10) coroutine.yield(20) return 30 end"
            );
            DynValue coroutine = script.CreateCoroutine(function);

            List<object> results = coroutine.Coroutine.AsEnumerable().ToList();

            Assert.That(results, Is.EqualTo(new object[] { 10d, 20d, 30d }));
        }

        [Test]
        public void AsEnumerableOfTReturnsTypedScalars()
        {
            Script script = new();
            DynValue function = script.DoString(
                "return function() coroutine.yield(1) coroutine.yield(2) return 3 end"
            );
            DynValue coroutine = script.CreateCoroutine(function);

            List<int> results = coroutine.Coroutine.AsEnumerable<int>().ToList();

            Assert.That(results, Is.EqualTo(YieldedValues));
        }

        [Test]
        public void AsUnityCoroutineYieldsNullPerIteration()
        {
            Script script = new();
            DynValue function = script.DoString(
                "return function() coroutine.yield('a') coroutine.yield('b') return 'c' end"
            );
            DynValue coroutine = script.CreateCoroutine(function);

            System.Collections.IEnumerator unityCoroutine = coroutine.Coroutine.AsUnityCoroutine();
            List<object> yielded = new();

            while (unityCoroutine.MoveNext())
            {
                yielded.Add(unityCoroutine.Current);
            }

            Assert.That(yielded, Has.Count.EqualTo(3));
            Assert.That(yielded.TrueForAll(value => value == null), Is.True);
            Assert.That(coroutine.Coroutine.State, Is.EqualTo(CoroutineState.Dead));
        }

        [Test]
        public void MarkClrCallbackAsDeadTransitionsType()
        {
            Script script = new();
            DynValue callback = DynValue.NewCallback((_, _) => DynValue.Nil);
            DynValue coroutine = script.CreateCoroutine(callback);

            coroutine.Coroutine.MarkClrCallbackAsDead();

            Assert.That(coroutine.Coroutine.State, Is.EqualTo(CoroutineState.Dead));
        }

        [Test]
        public void StateTransitionsFollowCoroutineLifecycle()
        {
            Script script = new();
            DynValue function = script.DoString(
                "return function() coroutine.yield(1) coroutine.yield(2) end"
            );
            DynValue coroutine = script.CreateCoroutine(function);

            Assert.That(coroutine.Coroutine.State, Is.EqualTo(CoroutineState.NotStarted));

            DynValue first = coroutine.Coroutine.Resume();
            Assert.That(first.Number, Is.EqualTo(1));
            Assert.That(coroutine.Coroutine.State, Is.EqualTo(CoroutineState.Suspended));

            coroutine.Coroutine.Resume();
            coroutine.Coroutine.Resume();

            Assert.That(coroutine.Coroutine.State, Is.EqualTo(CoroutineState.Dead));
        }

        [Test]
        public void ResumeClrCallbackExecutesAndMarksDead()
        {
            Script script = new();
            ScriptExecutionContext context = TestHelpers.CreateExecutionContext(script);
            DynValue callback = DynValue.NewCallback(
                (ctx, args) =>
                {
                    Assert.That(ctx, Is.SameAs(context));
                    return args.Count > 0 ? args[0] : DynValue.NewNumber(99);
                }
            );
            DynValue coroutine = script.CreateCoroutine(callback);
            coroutine.Coroutine.OwnerScript = script;

            DynValue payload = DynValue.NewString("payload");
            DynValue result = coroutine.Coroutine.Resume(context, payload);

            Assert.That(result.String, Is.EqualTo("payload"));
            Assert.That(coroutine.Coroutine.State, Is.EqualTo(CoroutineState.Dead));
        }

        [Test]
        public void ResumeClrCallbackTwiceThrows()
        {
            Script script = new();
            ScriptExecutionContext context = TestHelpers.CreateExecutionContext(script);
            DynValue callback = DynValue.NewCallback((ctx, _) => DynValue.NewNumber(1));
            DynValue coroutine = script.CreateCoroutine(callback);
            coroutine.Coroutine.OwnerScript = script;

            DynValue first = coroutine.Coroutine.Resume(context);
            Assert.That(first.Number, Is.EqualTo(1));

            ScriptRuntimeException exception = Assert.Throws<ScriptRuntimeException>(() =>
                coroutine.Coroutine.Resume(context)
            );
            Assert.That(exception.Message, Does.Contain("cannot resume dead coroutine"));
        }

        [Test]
        public void ResumeWithExplicitContextUsesDefaultArguments()
        {
            Script script = new();
            DynValue function = script.DoString("return function() return 5 end");
            DynValue coroutine = script.CreateCoroutine(function);
            ScriptExecutionContext context = TestHelpers.CreateExecutionContext(script);

            DynValue result = coroutine.Coroutine.Resume(context);

            Assert.That(result.Number, Is.EqualTo(5));
            Assert.That(coroutine.Coroutine.State, Is.EqualTo(CoroutineState.Dead));
        }

        [Test]
        public void ResumeWithObjectArgumentsConvertsValues()
        {
            Script script = new();
            DynValue function = script.DoString("return function(a, b) return a + b end");
            DynValue coroutine = script.CreateCoroutine(function);

            DynValue result = coroutine.Coroutine.Resume(40, 2);

            Assert.That(result.Number, Is.EqualTo(42));
        }

        [Test]
        public void ResumeWithContextObjectArgumentsConvertsValues()
        {
            Script script = new();
            DynValue function = script.DoString("return function(a, b) return a + b end");
            DynValue coroutine = script.CreateCoroutine(function);
            ScriptExecutionContext context = TestHelpers.CreateExecutionContext(script);

            DynValue result = coroutine.Coroutine.Resume(context, 30, 12);

            Assert.That(result.Number, Is.EqualTo(42));
            Assert.That(coroutine.Coroutine.State, Is.EqualTo(CoroutineState.Dead));
        }

        [Test]
        public void ResumeWithObjectArgumentsOnClrCallbackThrows()
        {
            Script script = new();
            DynValue callback = DynValue.NewCallback((_, _) => DynValue.Nil);
            DynValue coroutine = script.CreateCoroutine(callback);

            InvalidOperationException exception = Assert.Throws<InvalidOperationException>(() =>
                coroutine.Coroutine.Resume("value")
            );

            Assert.That(exception.Message, Does.Contain("Only non-CLR coroutines"));
        }

        [Test]
        public void ResumeWithDynValueArgumentsThrowsWhenNull()
        {
            Script script = new();
            DynValue function = script.DoString("return function() return 1 end");
            DynValue coroutine = script.CreateCoroutine(function);

            Assert.That(
                () => coroutine.Coroutine.Resume((DynValue[])null),
                Throws.ArgumentNullException.With.Property("ParamName").EqualTo("args")
            );
        }

        [Test]
        public void ResumeWithDynValueArgumentsThrowsForClrCallbacks()
        {
            Script script = new();
            DynValue callback = DynValue.NewCallback((_, _) => DynValue.Nil);
            DynValue coroutine = script.CreateCoroutine(callback);

            InvalidOperationException exception = Assert.Throws<InvalidOperationException>(() =>
                coroutine.Coroutine.Resume(Array.Empty<DynValue>())
            );
            Assert.That(exception.Message, Does.Contain("Only non-CLR coroutines"));
        }

        [Test]
        public void ResumeWithContextArgsThrowsWhenContextNull()
        {
            Script script = new();
            DynValue function = script.DoString("return function() return 1 end");
            DynValue coroutine = script.CreateCoroutine(function);

            Assert.That(
                () => coroutine.Coroutine.Resume(null, Array.Empty<DynValue>()),
                Throws.ArgumentNullException.With.Property("ParamName").EqualTo("context")
            );
        }

        [Test]
        public void ResumeWithContextArgsThrowsWhenArgsNull()
        {
            Script script = new();
            DynValue function = script.DoString("return function() return 1 end");
            DynValue coroutine = script.CreateCoroutine(function);
            ScriptExecutionContext context = TestHelpers.CreateExecutionContext(script);

            Assert.That(
                () => coroutine.Coroutine.Resume(context, null),
                Throws.ArgumentNullException.With.Property("ParamName").EqualTo("args")
            );
        }

        [Test]
        public void ResumeWithObjectArgsThrowsWhenArgsNull()
        {
            Script script = new();
            DynValue function = script.DoString("return function() return 1 end");
            DynValue coroutine = script.CreateCoroutine(function);

            Assert.That(
                () => coroutine.Coroutine.Resume((object[])null),
                Throws.ArgumentNullException.With.Property("ParamName").EqualTo("args")
            );
        }

        [Test]
        public void ResumeWithContextObjectArgsThrowsWhenArgsNull()
        {
            Script script = new();
            DynValue function = script.DoString("return function() return 1 end");
            DynValue coroutine = script.CreateCoroutine(function);
            ScriptExecutionContext context = TestHelpers.CreateExecutionContext(script);

            Assert.That(
                () => coroutine.Coroutine.Resume(context, (object[])null),
                Throws.ArgumentNullException.With.Property("ParamName").EqualTo("args")
            );
        }

        [Test]
        public void AutoYieldCounterProxiesProcessorValue()
        {
            Script script = new();
            DynValue function = script.DoString("return function() coroutine.yield(1) end");
            DynValue coroutine = script.CreateCoroutine(function);

            coroutine.Coroutine.AutoYieldCounter = 42;

            Assert.That(coroutine.Coroutine.AutoYieldCounter, Is.EqualTo(42));
        }

        [Test]
        public void GetProcessorForTestsThrowsForClrCallbacks()
        {
            Script script = new();
            DynValue callback = DynValue.NewCallback((_, _) => DynValue.Nil);
            DynValue coroutine = script.CreateCoroutine(callback);

            Assert.That(
                () => coroutine.Coroutine.GetProcessorForTests(),
                Throws.InvalidOperationException.With.Message.Contains("CLR callback")
            );
        }

        [Test]
        public void ForceStateForTestsThrowsForClrCallbacks()
        {
            Script script = new();
            DynValue callback = DynValue.NewCallback((_, _) => DynValue.Nil);
            DynValue coroutine = script.CreateCoroutine(callback);

            Assert.That(
                () => coroutine.Coroutine.ForceStateForTests(CoroutineState.Suspended),
                Throws.InvalidOperationException.With.Message.Contains("CLR callback")
            );
        }

        [Test]
        public void CloseReturnsTrueForClrCallbacks()
        {
            Script script = new();
            DynValue callback = DynValue.NewCallback((_, _) => DynValue.Nil);
            DynValue coroutine = script.CreateCoroutine(callback);

            DynValue result = coroutine.Coroutine.Close();

            Assert.That(result.Type, Is.EqualTo(DataType.Boolean));
            Assert.That(result.Boolean, Is.True);
            Assert.That(coroutine.Coroutine.State, Is.EqualTo(CoroutineState.NotStarted));
        }
    }
}
