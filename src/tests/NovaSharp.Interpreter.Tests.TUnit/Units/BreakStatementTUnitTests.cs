namespace NovaSharp.Interpreter.Tests.TUnit.Units
{
    using System.Threading.Tasks;
    using global::TUnit.Assertions;
    using NovaSharp.Interpreter;
    using NovaSharp.Interpreter.DataTypes;
    using NovaSharp.Interpreter.Errors;

    public sealed class BreakStatementTUnitTests
    {
        [global::TUnit.Core.Test]
        public async Task BreakOutsideLoopThrowsSyntaxError()
        {
            Script script = new();

            SyntaxErrorException exception = Assert.Throws<SyntaxErrorException>(() =>
                script.DoString("break")
            );

            await Assert
                .That(exception.DecoratedMessage)
                .Contains("not inside a loop")
                .ConfigureAwait(false);
        }

        [global::TUnit.Core.Test]
        public async Task BreakInsideStandaloneFunctionTriggersLoopBoundaryGuard()
        {
            Script script = new();
            const string chunk =
                @"
                local function inner()
                    break
                end
            ";

            SyntaxErrorException exception = Assert.Throws<SyntaxErrorException>(() =>
                script.DoString(chunk)
            );

            await Assert
                .That(exception.DecoratedMessage)
                .Contains("not inside a loop")
                .ConfigureAwait(false);
        }

        [global::TUnit.Core.Test]
        public async Task BreakInsideNestedFunctionDefinedInLoopCannotEscapeOuterLoop()
        {
            Script script = new();
            const string chunk =
                @"
                for i = 1, 2 do
                    local function inner()
                        break
                    end
                end
            ";

            SyntaxErrorException exception = Assert.Throws<SyntaxErrorException>(() =>
                script.DoString(chunk)
            );

            await Assert
                .That(exception.DecoratedMessage)
                .Contains("not inside a loop")
                .ConfigureAwait(false);
        }

        [global::TUnit.Core.Test]
        public async Task MultipleBreakStatementsRespectInnermostLoopScope()
        {
            Script script = new();
            const string chunk =
                @"
                local log = {}

                local function emit(flag)
                    while true do
                        if flag then
                            table.insert(log, 'first')
                            break
                        end

                        table.insert(log, 'second')
                        break
                    end
                end

                emit(true)
                emit(false)
                return log
            ";

            DynValue result = script.DoString(chunk);
            Table log = result.Table;

            await Assert.That(log.Length).IsEqualTo(2).ConfigureAwait(false);
            await Assert.That(log.Get(1).String).IsEqualTo("first").ConfigureAwait(false);
            await Assert.That(log.Get(2).String).IsEqualTo("second").ConfigureAwait(false);
        }

        [global::TUnit.Core.Test]
        public async Task BreakOnlyExitsInnermostLoop()
        {
            Script script = new();
            const string chunk =
                @"
                local log = {}
                for outer = 1, 3 do
                    for inner = 1, 3 do
                        table.insert(log, string.format('%d-%d', outer, inner))
                        break
                    end
                end
                return log
            ";

            DynValue result = script.DoString(chunk);
            Table log = result.Table;

            await Assert.That(log.Length).IsEqualTo(3).ConfigureAwait(false);
            await Assert.That(log.Get(1).String).IsEqualTo("1-1").ConfigureAwait(false);
            await Assert.That(log.Get(2).String).IsEqualTo("2-1").ConfigureAwait(false);
            await Assert.That(log.Get(3).String).IsEqualTo("3-1").ConfigureAwait(false);
        }
    }
}
