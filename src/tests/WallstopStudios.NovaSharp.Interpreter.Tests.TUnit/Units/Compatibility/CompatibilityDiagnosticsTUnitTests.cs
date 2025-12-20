namespace WallstopStudios.NovaSharp.Interpreter.Tests.TUnit.Units.Compatibility
{
    using System.Threading.Tasks;
    using global::TUnit.Assertions;
    using global::TUnit.Core;
    using WallstopStudios.NovaSharp.Interpreter;
    using WallstopStudios.NovaSharp.Interpreter.Compatibility;
    using WallstopStudios.NovaSharp.Interpreter.Errors;

    public sealed class CompatibilityDiagnosticsTUnitTests
    {
        [global::TUnit.Core.Test]
        [Arguments(LuaCompatibilityVersion.Lua52)]
        public async Task SyntaxErrorDecoratedMessageIncludesCompatibilityProfile(
            LuaCompatibilityVersion version
        )
        {
            Script script = new(version);

            SyntaxErrorException exception = Assert.Throws<SyntaxErrorException>(() =>
                script.DoString("local = 1")
            );

            await Assert
                .That(exception.DecoratedMessage)
                .Contains("[compatibility: Lua 5.2]")
                .ConfigureAwait(false);
        }

        [global::TUnit.Core.Test]
        [Arguments(LuaCompatibilityVersion.Lua55)]
        public async Task RuntimeErrorDecoratedMessageIncludesCompatibilityProfile(
            LuaCompatibilityVersion version
        )
        {
            Script script = new(version);

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
