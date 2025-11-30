#pragma warning disable CA2007
namespace NovaSharp.Interpreter.Tests.TUnit.Units
{
    using System.Threading.Tasks;
    using global::TUnit.Assertions;
    using NovaSharp.Interpreter;
    using NovaSharp.Interpreter.Compatibility;
    using NovaSharp.Interpreter.Errors;

    public sealed class CompatibilityDiagnosticsTUnitTests
    {
        [global::TUnit.Core.Test]
        public async Task SyntaxErrorDecoratedMessageIncludesCompatibilityProfile()
        {
            Script script = new(
                new ScriptOptions { CompatibilityVersion = LuaCompatibilityVersion.Lua52 }
            );

            SyntaxErrorException exception = Assert.Throws<SyntaxErrorException>(() =>
                script.DoString("local = 1")
            );

            await Assert.That(exception.DecoratedMessage).Contains("[compatibility: Lua 5.2]");
        }

        [global::TUnit.Core.Test]
        public async Task RuntimeErrorDecoratedMessageIncludesCompatibilityProfile()
        {
            Script script = new(
                new ScriptOptions { CompatibilityVersion = LuaCompatibilityVersion.Lua55 }
            );

            ScriptRuntimeException exception = Assert.Throws<ScriptRuntimeException>(() =>
                script.DoString("error('boom')")
            );

            await Assert.That(exception.DecoratedMessage).Contains("[compatibility: Lua 5.5]");
        }
    }
}
#pragma warning restore CA2007
