namespace NovaSharp.Interpreter.Tests.Units
{
    using NovaSharp.Interpreter;
    using NovaSharp.Interpreter.Compatibility;
    using NovaSharp.Interpreter.Errors;
    using NUnit.Framework;

    [TestFixture]
    public class CompatibilityDiagnosticsTests
    {
        [Test]
        public void SyntaxErrorDecoratedMessageIncludesCompatibilityProfile()
        {
            Script script = new(
                new ScriptOptions { CompatibilityVersion = LuaCompatibilityVersion.Lua52 }
            );

            SyntaxErrorException exception = Assert.Throws<SyntaxErrorException>(() =>
                script.DoString("local = 1")
            )!;

            Assert.That(exception.DecoratedMessage, Does.Contain("[compatibility: Lua 5.2]"));
        }

        [Test]
        public void RuntimeErrorDecoratedMessageIncludesCompatibilityProfile()
        {
            Script script = new(
                new ScriptOptions { CompatibilityVersion = LuaCompatibilityVersion.Lua55 }
            );

            ScriptRuntimeException exception = Assert.Throws<ScriptRuntimeException>(() =>
                script.DoString("error('boom')")
            )!;

            Assert.That(exception.DecoratedMessage, Does.Contain("[compatibility: Lua 5.5]"));
        }
    }
}
