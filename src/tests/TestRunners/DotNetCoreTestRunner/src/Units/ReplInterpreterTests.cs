namespace NovaSharp.Interpreter.Tests.Units
{
    using NUnit.Framework;
    using REPL;

    [TestFixture]
    public class ReplInterpreterTests
    {
        [Test]
        public void ClassicPromptReflectsPendingState()
        {
            ReplInterpreter interpreter = new(new Script(CoreModules.PresetComplete));
            Assert.That(interpreter.ClassicPrompt, Is.EqualTo(">"));

            interpreter.Evaluate("function foo()");
            Assert.Multiple(() =>
            {
                Assert.That(interpreter.HasPendingCommand, Is.True);
                Assert.That(interpreter.ClassicPrompt, Is.EqualTo(">>"));
            });
        }

        [Test]
        public void EvaluateSupportsClassicExpressionSyntax()
        {
            ReplInterpreter interpreter = new(new Script(CoreModules.PresetComplete))
            {
                HandleClassicExprsSyntax = true,
            };

            DynValue result = interpreter.Evaluate("=1 + 41");
            Assert.Multiple(() =>
            {
                Assert.That(result.Type, Is.EqualTo(DataType.Number));
                Assert.That(result.Number, Is.EqualTo(42));
            });
        }

        [Test]
        public void EvaluateSupportsDynamicExpressionSyntax()
        {
            ReplInterpreter interpreter = new(new Script(CoreModules.PresetComplete))
            {
                HandleDynamicExprs = true,
            };

            interpreter.Evaluate("x = 10");
            interpreter.Evaluate("");

            DynValue result = interpreter.Evaluate("?x * 2");
            Assert.Multiple(() =>
            {
                Assert.That(result.Type, Is.EqualTo(DataType.Number));
                Assert.That(result.Number, Is.EqualTo(20));
            });
        }

        [Test]
        public void EvaluateReturnsNullWhenAwaitingMoreInput()
        {
            ReplInterpreter interpreter = new(new Script(CoreModules.PresetComplete));

            DynValue first = interpreter.Evaluate("function foo()");
            Assert.Multiple(() =>
            {
                Assert.That(first, Is.Null);
                Assert.That(interpreter.HasPendingCommand, Is.True);
            });

            DynValue second = interpreter.Evaluate("return 99 end");
            Assert.Multiple(() =>
            {
                Assert.That(second, Is.Not.Null);
                Assert.That(second.Type, Is.EqualTo(DataType.Void));
            });

            DynValue third = interpreter.Evaluate("return foo()");
            Assert.Multiple(() =>
            {
                Assert.That(third.Type, Is.EqualTo(DataType.Number));
                Assert.That(third.Number, Is.EqualTo(99));
                Assert.That(interpreter.HasPendingCommand, Is.False);
            });
        }
    }
}
