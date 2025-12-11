namespace WallstopStudios.NovaSharp.Interpreter.Tests.TUnit.Units.Compatibility
{
    using System.Threading.Tasks;
    using global::TUnit.Assertions;
    using WallstopStudios.NovaSharp.Interpreter;
    using WallstopStudios.NovaSharp.Interpreter.Compatibility;
    using WallstopStudios.NovaSharp.Interpreter.Errors;

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

            await Assert
                .That(exception.DecoratedMessage)
                .Contains("[compatibility: Lua 5.2]")
                .ConfigureAwait(false);
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

            await Assert
                .That(exception.DecoratedMessage)
                .Contains("[compatibility: Lua 5.5]")
                .ConfigureAwait(false);
        }
    }
}
