namespace NovaSharp.Interpreter.Tests.Units
{
    using NovaSharp.Interpreter;
    using NovaSharp.Interpreter.Modules;
    using NovaSharp.Interpreter.REPL;
    using NUnit.Framework;

    [TestFixture]
    public sealed class ReplHistoryInterpreterTests
    {
        [Test]
        public void HistoryTracksPreviousEntries()
        {
            Script script = new Script(CoreModules.None);
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
            Script script = new Script(CoreModules.None);
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
    }
}
