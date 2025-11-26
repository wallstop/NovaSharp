namespace NovaSharp.Interpreter.Tests.Units
{
    using System;
    using NovaSharp.Interpreter;
    using NovaSharp.Interpreter.DataTypes;
    using NovaSharp.Interpreter.Errors;
    using NovaSharp.Interpreter.Modules;
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
        public void CurrentPendingCommandExposesBufferedInput()
        {
            ReplInterpreter interpreter = new(new Script(CoreModules.PresetComplete));

            DynValue pending = interpreter.Evaluate("function sample()");
            Assert.That(pending, Is.Null);

            Assert.That(interpreter.CurrentPendingCommand, Does.StartWith("function sample()"));

            // Finish the command to reset state for later tests
            Assert.That(() => interpreter.Evaluate("end"), Throws.Nothing);
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

        [Test]
        public void DynamicExpressionReturnsComputedValueWhenCodeIsPresent()
        {
            ReplInterpreter interpreter = new(new Script(CoreModules.PresetComplete))
            {
                HandleDynamicExprs = true,
            };

            DynValue result = interpreter.Evaluate("?1 + 41");

            Assert.Multiple(() =>
            {
                Assert.That(result.Type, Is.EqualTo(DataType.Number));
                Assert.That(result.Number, Is.EqualTo(42));
                Assert.That(interpreter.HasPendingCommand, Is.False);
            });
        }

        [Test]
        public void EmptyInputWithoutPendingReturnsVoid()
        {
            ReplInterpreter interpreter = new(new Script(CoreModules.PresetComplete));

            DynValue result = interpreter.Evaluate(string.Empty);

            Assert.Multiple(() =>
            {
                Assert.That(result, Is.EqualTo(DynValue.Void));
                Assert.That(interpreter.HasPendingCommand, Is.False);
            });
        }

        [Test]
        public void DynamicExpressionHandlesEmptyCodeAsVoid()
        {
            ReplInterpreter interpreter = new(new Script(CoreModules.PresetComplete))
            {
                HandleDynamicExprs = true,
            };

            interpreter.Evaluate("x = 7");
            interpreter.Evaluate(string.Empty);

            DynValue result = interpreter.Evaluate("?   ");
            Assert.Multiple(() =>
            {
                Assert.That(result, Is.EqualTo(DynValue.Void));
                Assert.That(interpreter.HasPendingCommand, Is.False);
            });
        }

        [Test]
        public void ForcedBlankLineRethrowsPrematureSyntaxError()
        {
            ReplInterpreter interpreter = new(new Script(CoreModules.PresetComplete));

            DynValue pending = interpreter.Evaluate("function foo()");
            Assert.That(pending, Is.Null);
            Assert.That(interpreter.HasPendingCommand, Is.True);

            Assert.Throws<SyntaxErrorException>(() => interpreter.Evaluate(string.Empty));
            Assert.That(interpreter.HasPendingCommand, Is.False);
        }

        [Test]
        public void SyntaxErrorsClearPendingCommand()
        {
            ReplInterpreter interpreter = new(new Script(CoreModules.PresetComplete));

            Assert.Throws<SyntaxErrorException>(() => interpreter.Evaluate("return )"));
            Assert.That(interpreter.HasPendingCommand, Is.False);
        }

        [Test]
        public void RuntimeExceptionsClearPendingCommand()
        {
            ReplInterpreter interpreter = new(new Script(CoreModules.PresetComplete));

            Assert.Throws<ScriptRuntimeException>(() => interpreter.Evaluate("error('boom')"));
            Assert.That(interpreter.HasPendingCommand, Is.False);
        }

        [Test]
        public void UnhandledExceptionsAlsoClearPendingCommand()
        {
            ReplInterpreter interpreter = new(new Script(CoreModules.PresetComplete));

            ReplInterpreter.TestHooks.SetScript(interpreter, null);

            Assert.Throws<NullReferenceException>(() => interpreter.Evaluate("return 5"));
            Assert.That(interpreter.HasPendingCommand, Is.False);
        }
    }
}
