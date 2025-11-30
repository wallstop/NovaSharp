#pragma warning disable CA2007
namespace NovaSharp.Interpreter.Tests.TUnit.Cli
{
    using System;
    using System.Threading.Tasks;
    using global::TUnit.Assertions;
    using NovaSharp.Interpreter;
    using NovaSharp.Interpreter.DataTypes;
    using NovaSharp.Interpreter.Errors;
    using NovaSharp.Interpreter.Modules;
    using NovaSharp.Interpreter.REPL;

    public sealed class ReplInterpreterTUnitTests
    {
        [global::TUnit.Core.Test]
        public async Task ClassicPromptReflectsPendingState()
        {
            ReplInterpreter interpreter = new(new Script(CoreModules.PresetComplete));

            await Assert.That(interpreter.ClassicPrompt).IsEqualTo(">");
            interpreter.Evaluate("function foo()");
            await Assert.That(interpreter.HasPendingCommand).IsTrue();
            await Assert.That(interpreter.ClassicPrompt).IsEqualTo(">>");
        }

        [global::TUnit.Core.Test]
        public async Task CurrentPendingCommandExposesBufferedInput()
        {
            ReplInterpreter interpreter = new(new Script(CoreModules.PresetComplete));

            DynValue pending = interpreter.Evaluate("function sample()");
            await Assert.That(pending).IsNull();
            await Assert.That(interpreter.CurrentPendingCommand).StartsWith("function sample()");
            DynValue completion = interpreter.Evaluate("end");
            await Assert.That(completion).IsEqualTo(DynValue.Void);
        }

        [global::TUnit.Core.Test]
        public async Task EvaluateSupportsClassicExpressionSyntax()
        {
            ReplInterpreter interpreter = new(new Script(CoreModules.PresetComplete))
            {
                HandleClassicExprsSyntax = true,
            };

            DynValue result = interpreter.Evaluate("=1 + 41");
            await Assert.That(result.Type).IsEqualTo(DataType.Number);
            await Assert.That(result.Number).IsEqualTo(42);
        }

        [global::TUnit.Core.Test]
        public async Task EvaluateSupportsDynamicExpressionSyntax()
        {
            ReplInterpreter interpreter = new(new Script(CoreModules.PresetComplete))
            {
                HandleDynamicExprs = true,
            };

            interpreter.Evaluate("x = 10");
            interpreter.Evaluate(string.Empty);

            DynValue result = interpreter.Evaluate("?x * 2");
            await Assert.That(result.Type).IsEqualTo(DataType.Number);
            await Assert.That(result.Number).IsEqualTo(20);
        }

        [global::TUnit.Core.Test]
        public async Task EvaluateReturnsNullWhenAwaitingMoreInput()
        {
            ReplInterpreter interpreter = new(new Script(CoreModules.PresetComplete));

            DynValue first = interpreter.Evaluate("function foo()");
            await Assert.That(first).IsNull();
            await Assert.That(interpreter.HasPendingCommand).IsTrue();

            DynValue second = interpreter.Evaluate("return 99 end");
            await Assert.That(second).IsNotNull();
            await Assert.That(second.Type).IsEqualTo(DataType.Void);

            DynValue third = interpreter.Evaluate("return foo()");
            await Assert.That(third.Type).IsEqualTo(DataType.Number);
            await Assert.That(third.Number).IsEqualTo(99);
            await Assert.That(interpreter.HasPendingCommand).IsFalse();
        }

        [global::TUnit.Core.Test]
        public async Task DynamicExpressionReturnsComputedValueWhenCodeIsPresent()
        {
            ReplInterpreter interpreter = new(new Script(CoreModules.PresetComplete))
            {
                HandleDynamicExprs = true,
            };

            DynValue result = interpreter.Evaluate("?1 + 41");
            await Assert.That(result.Type).IsEqualTo(DataType.Number);
            await Assert.That(result.Number).IsEqualTo(42);
            await Assert.That(interpreter.HasPendingCommand).IsFalse();
        }

        [global::TUnit.Core.Test]
        public async Task EmptyInputWithoutPendingReturnsVoid()
        {
            ReplInterpreter interpreter = new(new Script(CoreModules.PresetComplete));

            DynValue result = interpreter.Evaluate(string.Empty);
            await Assert.That(result).IsEqualTo(DynValue.Void);
            await Assert.That(interpreter.HasPendingCommand).IsFalse();
        }

        [global::TUnit.Core.Test]
        public async Task DynamicExpressionHandlesEmptyCodeAsVoid()
        {
            ReplInterpreter interpreter = new(new Script(CoreModules.PresetComplete))
            {
                HandleDynamicExprs = true,
            };

            interpreter.Evaluate("x = 7");
            interpreter.Evaluate(string.Empty);

            DynValue result = interpreter.Evaluate("?   ");
            await Assert.That(result).IsEqualTo(DynValue.Void);
            await Assert.That(interpreter.HasPendingCommand).IsFalse();
        }

        [global::TUnit.Core.Test]
        public async Task ForcedBlankLineRethrowsPrematureSyntaxError()
        {
            ReplInterpreter interpreter = new(new Script(CoreModules.PresetComplete));

            DynValue pending = interpreter.Evaluate("function foo()");
            await Assert.That(pending).IsNull();
            await Assert.That(interpreter.HasPendingCommand).IsTrue();

            SyntaxErrorException exception = ExpectException<SyntaxErrorException>(() =>
                interpreter.Evaluate(string.Empty)
            );
            await Assert.That(exception).IsNotNull();
            await Assert.That(interpreter.HasPendingCommand).IsFalse();
        }

        [global::TUnit.Core.Test]
        public async Task SyntaxErrorsClearPendingCommand()
        {
            ReplInterpreter interpreter = new(new Script(CoreModules.PresetComplete));

            SyntaxErrorException exception = ExpectException<SyntaxErrorException>(() =>
                interpreter.Evaluate("return )")
            );
            await Assert.That(exception).IsNotNull();
            await Assert.That(interpreter.HasPendingCommand).IsFalse();
        }

        [global::TUnit.Core.Test]
        public async Task RuntimeExceptionsClearPendingCommand()
        {
            ReplInterpreter interpreter = new(new Script(CoreModules.PresetComplete));

            ScriptRuntimeException exception = ExpectException<ScriptRuntimeException>(() =>
                interpreter.Evaluate("error('boom')")
            );
            await Assert.That(exception).IsNotNull();
            await Assert.That(interpreter.HasPendingCommand).IsFalse();
        }

        [global::TUnit.Core.Test]
        public async Task UnhandledExceptionsAlsoClearPendingCommand()
        {
            ReplInterpreter interpreter = new(new Script(CoreModules.PresetComplete));
            ReplInterpreter.TestHooks.SetScript(interpreter, null);

            NullReferenceException exception = ExpectException<NullReferenceException>(() =>
                interpreter.Evaluate("return 5")
            );
            await Assert.That(exception).IsNotNull();
            await Assert.That(interpreter.HasPendingCommand).IsFalse();
        }

        [global::TUnit.Core.Test]
        public async Task EvaluateThrowsWhenInputIsNull()
        {
            ReplInterpreter interpreter = new(new Script(CoreModules.PresetComplete));

            ArgumentNullException exception = ExpectException<ArgumentNullException>(() =>
                interpreter.Evaluate(null)
            );
            await Assert.That(exception.ParamName).IsEqualTo("input");
        }

        private static TException ExpectException<TException>(Action action)
            where TException : Exception
        {
            try
            {
                action();
            }
            catch (TException ex)
            {
                return ex;
            }

            throw new InvalidOperationException(
                $"Expected exception of type {typeof(TException).Name}."
            );
        }
    }
}
#pragma warning restore CA2007
