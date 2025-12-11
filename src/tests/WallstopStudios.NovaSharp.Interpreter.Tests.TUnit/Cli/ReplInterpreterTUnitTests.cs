namespace WallstopStudios.NovaSharp.Interpreter.Tests.TUnit.Cli
{
    using System;
    using System.Threading.Tasks;
    using global::TUnit.Assertions;
    using WallstopStudios.NovaSharp.Interpreter;
    using WallstopStudios.NovaSharp.Interpreter.DataTypes;
    using WallstopStudios.NovaSharp.Interpreter.Errors;
    using WallstopStudios.NovaSharp.Interpreter.Modules;
    using WallstopStudios.NovaSharp.Interpreter.REPL;

    public sealed class ReplInterpreterTUnitTests
    {
        [global::TUnit.Core.Test]
        public async Task ClassicPromptReflectsPendingState()
        {
            ReplInterpreter interpreter = new(new Script(CoreModulePresets.Complete));

            await Assert.That(interpreter.ClassicPrompt).IsEqualTo(">").ConfigureAwait(false);
            interpreter.Evaluate("function foo()");
            await Assert.That(interpreter.HasPendingCommand).IsTrue().ConfigureAwait(false);
            await Assert.That(interpreter.ClassicPrompt).IsEqualTo(">>").ConfigureAwait(false);
        }

        [global::TUnit.Core.Test]
        public async Task CurrentPendingCommandExposesBufferedInput()
        {
            ReplInterpreter interpreter = new(new Script(CoreModulePresets.Complete));

            DynValue pending = interpreter.Evaluate("function sample()");
            await Assert.That(pending).IsNull().ConfigureAwait(false);
            await Assert
                .That(interpreter.CurrentPendingCommand)
                .StartsWith("function sample()")
                .ConfigureAwait(false);
            DynValue completion = interpreter.Evaluate("end");
            await Assert.That(completion).IsEqualTo(DynValue.Void).ConfigureAwait(false);
        }

        [global::TUnit.Core.Test]
        public async Task EvaluateSupportsClassicExpressionSyntax()
        {
            ReplInterpreter interpreter = new(new Script(CoreModulePresets.Complete))
            {
                HandleClassicExprsSyntax = true,
            };

            DynValue result = interpreter.Evaluate("=1 + 41");
            await Assert.That(result.Type).IsEqualTo(DataType.Number).ConfigureAwait(false);
            await Assert.That(result.Number).IsEqualTo(42).ConfigureAwait(false);
        }

        [global::TUnit.Core.Test]
        public async Task EvaluateSupportsDynamicExpressionSyntax()
        {
            ReplInterpreter interpreter = new(new Script(CoreModulePresets.Complete))
            {
                HandleDynamicExprs = true,
            };

            interpreter.Evaluate("x = 10");
            interpreter.Evaluate(string.Empty);

            DynValue result = interpreter.Evaluate("?x * 2");
            await Assert.That(result.Type).IsEqualTo(DataType.Number).ConfigureAwait(false);
            await Assert.That(result.Number).IsEqualTo(20).ConfigureAwait(false);
        }

        [global::TUnit.Core.Test]
        public async Task EvaluateReturnsNullWhenAwaitingMoreInput()
        {
            ReplInterpreter interpreter = new(new Script(CoreModulePresets.Complete));

            DynValue first = interpreter.Evaluate("function foo()");
            await Assert.That(first).IsNull().ConfigureAwait(false);
            await Assert.That(interpreter.HasPendingCommand).IsTrue().ConfigureAwait(false);

            DynValue second = interpreter.Evaluate("return 99 end");
            await Assert.That(second).IsNotNull().ConfigureAwait(false);
            await Assert.That(second.Type).IsEqualTo(DataType.Void).ConfigureAwait(false);

            DynValue third = interpreter.Evaluate("return foo()");
            await Assert.That(third.Type).IsEqualTo(DataType.Number).ConfigureAwait(false);
            await Assert.That(third.Number).IsEqualTo(99).ConfigureAwait(false);
            await Assert.That(interpreter.HasPendingCommand).IsFalse().ConfigureAwait(false);
        }

        [global::TUnit.Core.Test]
        public async Task DynamicExpressionReturnsComputedValueWhenCodeIsPresent()
        {
            ReplInterpreter interpreter = new(new Script(CoreModulePresets.Complete))
            {
                HandleDynamicExprs = true,
            };

            DynValue result = interpreter.Evaluate("?1 + 41");
            await Assert.That(result.Type).IsEqualTo(DataType.Number).ConfigureAwait(false);
            await Assert.That(result.Number).IsEqualTo(42).ConfigureAwait(false);
            await Assert.That(interpreter.HasPendingCommand).IsFalse().ConfigureAwait(false);
        }

        [global::TUnit.Core.Test]
        public async Task EmptyInputWithoutPendingReturnsVoid()
        {
            ReplInterpreter interpreter = new(new Script(CoreModulePresets.Complete));

            DynValue result = interpreter.Evaluate(string.Empty);
            await Assert.That(result).IsEqualTo(DynValue.Void).ConfigureAwait(false);
            await Assert.That(interpreter.HasPendingCommand).IsFalse().ConfigureAwait(false);
        }

        [global::TUnit.Core.Test]
        public async Task DynamicExpressionHandlesEmptyCodeAsVoid()
        {
            ReplInterpreter interpreter = new(new Script(CoreModulePresets.Complete))
            {
                HandleDynamicExprs = true,
            };

            interpreter.Evaluate("x = 7");
            interpreter.Evaluate(string.Empty);

            DynValue result = interpreter.Evaluate("?   ");
            await Assert.That(result).IsEqualTo(DynValue.Void).ConfigureAwait(false);
            await Assert.That(interpreter.HasPendingCommand).IsFalse().ConfigureAwait(false);
        }

        [global::TUnit.Core.Test]
        public async Task ForcedBlankLineRethrowsPrematureSyntaxError()
        {
            ReplInterpreter interpreter = new(new Script(CoreModulePresets.Complete));

            DynValue pending = interpreter.Evaluate("function foo()");
            await Assert.That(pending).IsNull().ConfigureAwait(false);
            await Assert.That(interpreter.HasPendingCommand).IsTrue().ConfigureAwait(false);

            SyntaxErrorException exception = ExpectException<SyntaxErrorException>(() =>
                interpreter.Evaluate(string.Empty)
            );
            await Assert.That(exception).IsNotNull().ConfigureAwait(false);
            await Assert.That(interpreter.HasPendingCommand).IsFalse().ConfigureAwait(false);
        }

        [global::TUnit.Core.Test]
        public async Task SyntaxErrorsClearPendingCommand()
        {
            ReplInterpreter interpreter = new(new Script(CoreModulePresets.Complete));

            SyntaxErrorException exception = ExpectException<SyntaxErrorException>(() =>
                interpreter.Evaluate("return )")
            );
            await Assert.That(exception).IsNotNull().ConfigureAwait(false);
            await Assert.That(interpreter.HasPendingCommand).IsFalse().ConfigureAwait(false);
        }

        [global::TUnit.Core.Test]
        public async Task RuntimeExceptionsClearPendingCommand()
        {
            ReplInterpreter interpreter = new(new Script(CoreModulePresets.Complete));

            ScriptRuntimeException exception = ExpectException<ScriptRuntimeException>(() =>
                interpreter.Evaluate("error('boom')")
            );
            await Assert.That(exception).IsNotNull().ConfigureAwait(false);
            await Assert.That(interpreter.HasPendingCommand).IsFalse().ConfigureAwait(false);
        }

        [global::TUnit.Core.Test]
        public async Task UnhandledExceptionsAlsoClearPendingCommand()
        {
            ReplInterpreter interpreter = new(new Script(CoreModulePresets.Complete));
            ReplInterpreter.TestHooks.SetScript(interpreter, null);

            NullReferenceException exception = ExpectException<NullReferenceException>(() =>
                interpreter.Evaluate("return 5")
            );
            await Assert.That(exception).IsNotNull().ConfigureAwait(false);
            await Assert.That(interpreter.HasPendingCommand).IsFalse().ConfigureAwait(false);
        }

        [global::TUnit.Core.Test]
        public async Task EvaluateThrowsWhenInputIsNull()
        {
            ReplInterpreter interpreter = new(new Script(CoreModulePresets.Complete));

            ArgumentNullException exception = ExpectException<ArgumentNullException>(() =>
                interpreter.Evaluate(null)
            );
            await Assert.That(exception.ParamName).IsEqualTo("input").ConfigureAwait(false);
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
