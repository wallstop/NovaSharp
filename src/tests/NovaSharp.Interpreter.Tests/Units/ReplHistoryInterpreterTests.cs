namespace NovaSharp.Interpreter.Tests.Units
{
    using System;
    using NovaSharp.Interpreter;
    using NovaSharp.Interpreter.Modules;
    using NovaSharp.Interpreter.REPL;
    using NUnit.Framework;

    [TestFixture]
    public sealed class ReplHistoryInterpreterTests
    {
        private const CoreModules MinimalScriptModules = TestCoreModules.BasicGlobals;

        [Test]
        public void HistoryTracksPreviousEntries()
        {
            Script script = new Script(MinimalScriptModules);
            ReplHistoryInterpreter interpreter = new ReplHistoryInterpreter(script, historySize: 2);

            interpreter.Evaluate("return 1");
            interpreter.Evaluate("return 2");

            Assert.Multiple(() =>
            {
                Assert.That(interpreter.HistoryPrev(), Is.EqualTo("return 2"));
                Assert.That(interpreter.HistoryPrev(), Is.EqualTo("return 1"));
                Assert.That(interpreter.HistoryPrev(), Is.EqualTo("return 2"));
            });
        }

        [Test]
        public void HistoryNextMovesForwardThroughEntries()
        {
            Script script = new Script(MinimalScriptModules);
            ReplHistoryInterpreter interpreter = new ReplHistoryInterpreter(script, historySize: 3);

            interpreter.Evaluate("return 1");
            interpreter.Evaluate("return 2");
            interpreter.Evaluate("return 3");

            Assert.That(interpreter.HistoryPrev(), Is.EqualTo("return 3"));
            Assert.That(interpreter.HistoryPrev(), Is.EqualTo("return 2"));

            Assert.Multiple(() =>
            {
                Assert.That(interpreter.HistoryNext(), Is.EqualTo("return 3"));
                Assert.That(interpreter.HistoryNext(), Is.EqualTo("return 1"));
                Assert.That(interpreter.HistoryNext(), Is.EqualTo("return 2"));
            });
        }

        [Test]
        public void HistoryPrevReturnsNullWhenNoEntries()
        {
            Script script = new Script(MinimalScriptModules);
            ReplHistoryInterpreter interpreter = new ReplHistoryInterpreter(script, historySize: 2);

            Assert.That(interpreter.HistoryPrev(), Is.Null);
        }

        [Test]
        public void HistoryNextReturnsNullWhenNavigationNotStarted()
        {
            Script script = new Script(MinimalScriptModules);
            ReplHistoryInterpreter interpreter = new ReplHistoryInterpreter(script, historySize: 2);
            interpreter.Evaluate("return 1");

            Assert.That(interpreter.HistoryNext(), Is.Null);
        }

        [Test]
        public void EvaluateResetsNavigationAndOverwritesOldEntries()
        {
            Script script = new Script(MinimalScriptModules);
            ReplHistoryInterpreter interpreter = new ReplHistoryInterpreter(script, historySize: 2);

            interpreter.Evaluate("return 1");
            interpreter.Evaluate("return 2");

            Assert.That(interpreter.HistoryPrev(), Is.EqualTo("return 2"));
            Assert.That(interpreter.HistoryPrev(), Is.EqualTo("return 1"));

            interpreter.Evaluate("return 3");

            Assert.That(interpreter.HistoryNext(), Is.Null);
            Assert.That(interpreter.HistoryPrev(), Is.EqualTo("return 3"));
            Assert.That(interpreter.HistoryPrev(), Is.EqualTo("return 2"));
        }

        [Test]
        public void ConstructorThrowsWhenHistorySizeNotPositive()
        {
            Script script = new Script(MinimalScriptModules);

            Assert.That(
                () => new ReplHistoryInterpreter(script, historySize: 0),
                Throws
                    .InstanceOf<ArgumentOutOfRangeException>()
                    .With.Property(nameof(ArgumentOutOfRangeException.ParamName))
                    .EqualTo("historySize")
            );
        }
    }
}
