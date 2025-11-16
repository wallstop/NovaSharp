namespace NovaSharp.Interpreter.Tests.Units
{
    using NovaSharp.Interpreter;
    using NovaSharp.Interpreter.DataTypes;
    using NovaSharp.Interpreter.Errors;
    using NUnit.Framework;

    [TestFixture]
    public sealed class BreakStatementTests
    {
        [Test]
        public void BreakOutsideLoopThrowsSyntaxError()
        {
            Script script = new Script();

            SyntaxErrorException exception = Assert.Throws<SyntaxErrorException>(
                () => script.DoString("break")
            );

            Assert.That(
                exception.DecoratedMessage,
                Does.Contain("not inside a loop"),
                "Compiler should reject top-level break statements."
            );
        }

        [Test]
        public void BreakInsideStandaloneFunctionTriggersLoopBoundaryGuard()
        {
            Script script = new Script();
            const string chunk = @"
                local function inner()
                    break
                end
            ";

            SyntaxErrorException exception = Assert.Throws<SyntaxErrorException>(
                () => script.DoString(chunk)
            );
            Assert.That(
                exception.DecoratedMessage,
                Does.Contain("not inside a loop"),
                "Loop boundary guards should prevent functions from breaking outer loops."
            );
        }

        [Test]
        public void BreakInsideNestedFunctionDefinedInLoopCannotEscapeOuterLoop()
        {
            Script script = new Script();
            const string chunk = @"
                for i = 1, 2 do
                    local function inner()
                        break
                    end
                end
            ";

            SyntaxErrorException exception = Assert.Throws<SyntaxErrorException>(
                () => script.DoString(chunk)
            );
            Assert.That(
                exception.DecoratedMessage,
                Does.Contain("not inside a loop"),
                "Break statements cannot cross function boundaries even when defined inside loops."
            );
        }

        [Test]
        public void MultipleBreakStatementsRespectInnermostLoopScope()
        {
            Script script = new Script();
            const string chunk = @"
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

            Assert.Multiple(() =>
            {
                Assert.That(log.Length, Is.EqualTo(2));
                Assert.That(log.Get(1).String, Is.EqualTo("first"));
                Assert.That(log.Get(2).String, Is.EqualTo("second"));
            });
        }

        [Test]
        public void BreakOnlyExitsInnermostLoop()
        {
            Script script = new Script();
            const string chunk = @"
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

            Assert.Multiple(() =>
            {
                Assert.That(log.Length, Is.EqualTo(3));
                Assert.That(log.Get(1).String, Is.EqualTo("1-1"));
                Assert.That(log.Get(2).String, Is.EqualTo("2-1"));
                Assert.That(log.Get(3).String, Is.EqualTo("3-1"));
            });
        }
    }
}
