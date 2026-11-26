namespace WallstopStudios.NovaSharp.Interpreter.Tests.TUnit.Units.Execution
{
    using System.Threading.Tasks;
    using global::NovaSharp;
    using global::TUnit.Assertions;
    using WallstopStudios.NovaSharp.Interpreter.Compatibility;
    using WallstopStudios.NovaSharp.Interpreter.Errors;
    using WallstopStudios.NovaSharp.Tests.TestInfrastructure.TUnit;

    /// <summary>
    /// Covers Lua 5.4 const locals and the Lua 5.5 const for-loop control variables, which are
    /// compile-time write errors in the reference interpreters.
    /// </summary>
    public sealed class ForLoopConstVariableTUnitTests
    {
        [global::TUnit.Core.Test]
        [LuaVersionsFrom(LuaCompatibilityVersion.Lua55)]
        public async Task NumericForControlVariableRejectsAssignment(
            LuaCompatibilityVersion version
        )
        {
            Script script = new(version);
            SyntaxErrorException exception = Assert.Throws<SyntaxErrorException>(() =>
                script.DoString("for i = 1, 3 do i = i + 1 end")
            );

            await Assert
                .That(exception.Message)
                .Contains("attempt to assign to const variable 'i'")
                .ConfigureAwait(false);
        }

        [global::TUnit.Core.Test]
        [LuaVersionsFrom(LuaCompatibilityVersion.Lua55)]
        public async Task GenericForControlVariableRejectsAssignment(
            LuaCompatibilityVersion version
        )
        {
            Script script = new(version);
            SyntaxErrorException exception = Assert.Throws<SyntaxErrorException>(() =>
                script.DoString("for key, value in pairs({ 1 }) do key = key + 1 end")
            );

            await Assert
                .That(exception.Message)
                .Contains("attempt to assign to const variable 'key'")
                .ConfigureAwait(false);
        }

        [global::TUnit.Core.Test]
        [LuaVersionsFrom(LuaCompatibilityVersion.Lua55)]
        public async Task CapturedLoopVariableRejectsAssignment(LuaCompatibilityVersion version)
        {
            Script script = new(version);
            SyntaxErrorException exception = Assert.Throws<SyntaxErrorException>(() =>
                script.DoString(
                    "local function update() for i = 1, 3 do return function() i = 2 end end end"
                )
            );

            await Assert
                .That(exception.Message)
                .Contains("attempt to assign to const variable 'i'")
                .ConfigureAwait(false);
        }

        [global::TUnit.Core.Test]
        [LuaVersionsFrom(LuaCompatibilityVersion.Lua55)]
        public async Task FunctionDefinitionSyntaxRejectsConstLoopVariable(
            LuaCompatibilityVersion version
        )
        {
            Script script = new(version);
            SyntaxErrorException exception = Assert.Throws<SyntaxErrorException>(() =>
                script.DoString("for i = 1, 3 do function i() end end")
            );

            await Assert
                .That(exception.Message)
                .Contains("attempt to assign to const variable 'i'")
                .ConfigureAwait(false);
        }

        [global::TUnit.Core.Test]
        [LuaVersionsFrom(LuaCompatibilityVersion.Lua54)]
        public async Task FunctionDefinitionSyntaxRejectsConstLocal(LuaCompatibilityVersion version)
        {
            Script script = new(version);
            SyntaxErrorException exception = Assert.Throws<SyntaxErrorException>(() =>
                script.DoString("local value <const> = 1 function value() end")
            );

            await Assert
                .That(exception.Message)
                .Contains("attempt to assign to const variable 'value'")
                .ConfigureAwait(false);
        }

        [global::TUnit.Core.Test]
        [LuaVersionsFrom(LuaCompatibilityVersion.Lua55)]
        public async Task GenericForValueVariableRemainsWritable(LuaCompatibilityVersion version)
        {
            Script script = new(version);
            LuaValue result = script.DoString(
                "for key, value in pairs({ 1 }) do value = 'changed' end return 'ok'"
            );

            await Assert.That(result.String).IsEqualTo("ok").ConfigureAwait(false);
        }

        [global::TUnit.Core.Test]
        [LuaVersionsUntil(LuaCompatibilityVersion.Lua54)]
        public async Task LoopVariablesRemainWritableBeforeLua55(LuaCompatibilityVersion version)
        {
            Script script = new(version);
            LuaValue result = script.DoString(
                "for i = 1, 2 do i = i + 1 end for key, value in pairs({ 1 }) do key = 'x' value = 'y' end return 'ok'"
            );

            await Assert.That(result.String).IsEqualTo("ok").ConfigureAwait(false);
        }

        [global::TUnit.Core.Test]
        [LuaVersionsFrom(LuaCompatibilityVersion.Lua54)]
        public async Task CapturedConstLocalRejectsAssignment(LuaCompatibilityVersion version)
        {
            Script script = new(version);
            SyntaxErrorException exception = Assert.Throws<SyntaxErrorException>(() =>
                script.DoString("local value <const> = 1 local function update() value = 2 end")
            );

            await Assert
                .That(exception.Message)
                .Contains("attempt to assign to const variable 'value'")
                .ConfigureAwait(false);
        }

        [global::TUnit.Core.Test]
        [LuaVersionsFrom(LuaCompatibilityVersion.Lua54)]
        public async Task ConstLocalInitializationRemainsAllowed(LuaCompatibilityVersion version)
        {
            Script script = new(version);
            LuaValue result = script.DoString("local value <const> = 7 return value");

            await Assert.That(result.Number).IsEqualTo(7d).ConfigureAwait(false);
        }
    }
}
