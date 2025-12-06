namespace WallstopStudios.NovaSharp.Interpreter.Tests.TUnit.Cli
{
    using System;
    using System.Threading.Tasks;
    using global::TUnit.Assertions;
    using WallstopStudios.NovaSharp.Interpreter;
    using WallstopStudios.NovaSharp.Interpreter.Modules;
    using WallstopStudios.NovaSharp.Interpreter.REPL;

    public sealed class ReplHistoryInterpreterTUnitTests
    {
        private const CoreModules MinimalScriptModules =
            CoreModules.Basic | CoreModules.GlobalConsts;

        [global::TUnit.Core.Test]
        public async Task HistoryTracksPreviousEntries()
        {
            Script script = new(MinimalScriptModules);
            ReplHistoryInterpreter interpreter = new(script, historySize: 2);

            interpreter.Evaluate("return 1");
            interpreter.Evaluate("return 2");

            await Assert
                .That(interpreter.HistoryPrev())
                .IsEqualTo("return 2")
                .ConfigureAwait(false);
            await Assert
                .That(interpreter.HistoryPrev())
                .IsEqualTo("return 1")
                .ConfigureAwait(false);
            await Assert
                .That(interpreter.HistoryPrev())
                .IsEqualTo("return 2")
                .ConfigureAwait(false);
        }

        [global::TUnit.Core.Test]
        public async Task HistoryNextMovesForwardThroughEntries()
        {
            Script script = new(MinimalScriptModules);
            ReplHistoryInterpreter interpreter = new(script, historySize: 3);

            interpreter.Evaluate("return 1");
            interpreter.Evaluate("return 2");
            interpreter.Evaluate("return 3");

            await Assert
                .That(interpreter.HistoryPrev())
                .IsEqualTo("return 3")
                .ConfigureAwait(false);
            await Assert
                .That(interpreter.HistoryPrev())
                .IsEqualTo("return 2")
                .ConfigureAwait(false);

            await Assert
                .That(interpreter.HistoryNext())
                .IsEqualTo("return 3")
                .ConfigureAwait(false);
            await Assert
                .That(interpreter.HistoryNext())
                .IsEqualTo("return 1")
                .ConfigureAwait(false);
            await Assert
                .That(interpreter.HistoryNext())
                .IsEqualTo("return 2")
                .ConfigureAwait(false);
        }

        [global::TUnit.Core.Test]
        public async Task HistoryPrevReturnsNullWhenNoEntries()
        {
            Script script = new(MinimalScriptModules);
            ReplHistoryInterpreter interpreter = new(script, historySize: 2);

            await Assert.That(interpreter.HistoryPrev()).IsNull().ConfigureAwait(false);
        }

        [global::TUnit.Core.Test]
        public async Task HistoryNextReturnsNullWhenNavigationNotStarted()
        {
            Script script = new(MinimalScriptModules);
            ReplHistoryInterpreter interpreter = new(script, historySize: 2);
            interpreter.Evaluate("return 1");

            await Assert.That(interpreter.HistoryNext()).IsNull().ConfigureAwait(false);
        }

        [global::TUnit.Core.Test]
        public async Task EvaluateResetsNavigationAndOverwritesOldEntries()
        {
            Script script = new(MinimalScriptModules);
            ReplHistoryInterpreter interpreter = new(script, historySize: 2);

            interpreter.Evaluate("return 1");
            interpreter.Evaluate("return 2");

            await Assert
                .That(interpreter.HistoryPrev())
                .IsEqualTo("return 2")
                .ConfigureAwait(false);
            await Assert
                .That(interpreter.HistoryPrev())
                .IsEqualTo("return 1")
                .ConfigureAwait(false);

            interpreter.Evaluate("return 3");

            await Assert.That(interpreter.HistoryNext()).IsNull().ConfigureAwait(false);
            await Assert
                .That(interpreter.HistoryPrev())
                .IsEqualTo("return 3")
                .ConfigureAwait(false);
            await Assert
                .That(interpreter.HistoryPrev())
                .IsEqualTo("return 2")
                .ConfigureAwait(false);
        }

        [global::TUnit.Core.Test]
        public async Task ConstructorThrowsWhenHistorySizeNotPositive()
        {
            Script script = new(MinimalScriptModules);

            ArgumentOutOfRangeException exception = ExpectException<ArgumentOutOfRangeException>(
                () =>
                    CreateInterpreter(script, historySize: 0)
            );

            await Assert.That(exception.ParamName).IsEqualTo("historySize").ConfigureAwait(false);
        }

        private static ReplHistoryInterpreter CreateInterpreter(Script script, int historySize)
        {
            return new ReplHistoryInterpreter(script, historySize);
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
