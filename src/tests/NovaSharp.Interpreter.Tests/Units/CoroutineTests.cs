namespace NovaSharp.Interpreter.Tests.Units
{
    using System.Collections.Generic;
    using System.Linq;
    using NovaSharp.Interpreter;
    using NovaSharp.Interpreter.DataTypes;
    using NovaSharp.Interpreter.Errors;
    using NovaSharp.Interpreter.Execution.VM;
    using NUnit.Framework;

    [TestFixture]
    public sealed class CoroutineTests
    {
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

            Assert.That(results, Is.EqualTo(new[] { 1, 2, 3 }));
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
    }
}
