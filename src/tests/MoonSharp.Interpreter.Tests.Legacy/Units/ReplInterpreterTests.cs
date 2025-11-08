using MoonSharp.Interpreter;
using MoonSharp.Interpreter.REPL;
using NUnit.Framework;

namespace MoonSharp.Interpreter.Tests.Units
{
    [TestFixture]
    public class ReplInterpreterTests
    {
        [Test]
        public void ClassicPromptReflectsPendingState()
        {
            var interpreter = new ReplInterpreter(new Script(CoreModules.Preset_Complete));
            Assert.That(interpreter.ClassicPrompt, Is.EqualTo(">"));

            interpreter.Evaluate("function foo()");
            Assert.That(interpreter.HasPendingCommand, Is.True);
            Assert.That(interpreter.ClassicPrompt, Is.EqualTo(">>"));
        }

        [Test]
        public void EvaluateSupportsClassicExpressionSyntax()
        {
            var interpreter = new ReplInterpreter(new Script(CoreModules.Preset_Complete))
            {
                HandleClassicExprsSyntax = true,
            };

            DynValue result = interpreter.Evaluate("=1 + 41");
            Assert.That(result.Type, Is.EqualTo(DataType.Number));
            Assert.That(result.Number, Is.EqualTo(42));
        }

        [Test]
        public void EvaluateSupportsDynamicExpressionSyntax()
        {
            var interpreter = new ReplInterpreter(new Script(CoreModules.Preset_Complete))
            {
                HandleDynamicExprs = true,
            };

            interpreter.Evaluate("x = 10");
            interpreter.Evaluate("");

            DynValue result = interpreter.Evaluate("?x * 2");
            Assert.That(result.Type, Is.EqualTo(DataType.Number));
            Assert.That(result.Number, Is.EqualTo(20));
        }

        [Test]
        public void EvaluateReturnsNullWhenAwaitingMoreInput()
        {
            var interpreter = new ReplInterpreter(new Script(CoreModules.Preset_Complete));

            DynValue first = interpreter.Evaluate("function foo()");
            Assert.That(first, Is.Null);
            Assert.That(interpreter.HasPendingCommand, Is.True);

            DynValue second = interpreter.Evaluate("return 99 end");
            Assert.That(second, Is.Not.Null);
            Assert.That(second.Type, Is.EqualTo(DataType.Void));

            DynValue third = interpreter.Evaluate("return foo()");
            Assert.That(third.Type, Is.EqualTo(DataType.Number));
            Assert.That(third.Number, Is.EqualTo(99));
            Assert.That(interpreter.HasPendingCommand, Is.False);
        }
    }
}
