namespace NovaSharp.Interpreter.Tests.Units
{
    using NovaSharp.Interpreter;
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
    }
}
