namespace NovaSharp.Interpreter.Tests.Units
{
    using NovaSharp;
    using NovaSharp.Interpreter;
    using NovaSharp.Interpreter.DataTypes;
    using NovaSharp.Interpreter.Errors;
    using NovaSharp.Interpreter.Modules;
    using NUnit.Framework;

    [TestFixture]
    public sealed class CoroutineCloseTests
    {
        [Test]
        public void CloseBeforeStartReturnsTrueAndMarksDead()
        {
            Script script = new(CoreModules.PresetComplete);
            script.DoString("function ready() return 1 end");

            DynValue coroutineValue = script.CreateCoroutine(script.Globals.Get("ready"));

            DynValue closeResult = coroutineValue.Coroutine.Close();

            Assert.Multiple(() =>
            {
                Assert.That(closeResult.Type, Is.EqualTo(DataType.Boolean));
                Assert.That(closeResult.Boolean, Is.True);
                Assert.That(coroutineValue.Coroutine.State, Is.EqualTo(CoroutineState.Dead));
            });
        }

        [Test]
        public void CloseWhileSuspendedEndsCoroutine()
        {
            Script script = new(CoreModules.PresetComplete);
            script.DoString(
                @"
                function pause()
                  coroutine.yield('pause')
                  return 'done'
                end
            "
            );

            DynValue coroutineValue = script.CreateCoroutine(script.Globals.Get("pause"));
            DynValue first = coroutineValue.Coroutine.Resume();

            Assert.That(first.Type, Is.EqualTo(DataType.String));
            Assert.That(first.String, Is.EqualTo("pause"));
            Assert.That(coroutineValue.Coroutine.State, Is.EqualTo(CoroutineState.Suspended));

            DynValue closeResult = coroutineValue.Coroutine.Close();

            Assert.Multiple(() =>
            {
                Assert.That(closeResult.Type, Is.EqualTo(DataType.Boolean));
                Assert.That(closeResult.Boolean, Is.True);
                Assert.That(coroutineValue.Coroutine.State, Is.EqualTo(CoroutineState.Dead));
            });

            Assert.That(
                () => coroutineValue.Coroutine.Resume(),
                Throws
                    .TypeOf<ScriptRuntimeException>()
                    .With.Message.Contains("cannot resume dead coroutine")
            );
        }

        [Test]
        public void CloseAfterExceptionReturnsFalseTuple()
        {
            Script script = new(CoreModules.PresetComplete);
            script.DoString(
                @"
                function blow()
                  error('boom!')
                end
            "
            );

            DynValue coroutineValue = script.CreateCoroutine(script.Globals.Get("blow"));

            Assert.That(
                () => coroutineValue.Coroutine.Resume(),
                Throws.TypeOf<ScriptRuntimeException>().With.Message.Contains("boom")
            );
            Assert.That(coroutineValue.Coroutine.State, Is.EqualTo(CoroutineState.Dead));

            DynValue closeResult = coroutineValue.Coroutine.Close();

            Assert.Multiple(() =>
            {
                Assert.That(closeResult.Type, Is.EqualTo(DataType.Tuple));
                Assert.That(closeResult.Tuple.Length, Is.EqualTo(2));
                Assert.That(closeResult.Tuple[0].Boolean, Is.False);
                Assert.That(closeResult.Tuple[1].String, Does.Contain("boom"));
            });
        }
    }
}
