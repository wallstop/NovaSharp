-- @lua-versions: 5.1, 5.5
-- @novasharp-only: false
-- @expects-error: true
-- @source: src/tests/WallstopStudios.NovaSharp.Interpreter.Tests.TUnit/Units/Execution/ScriptOptionsTUnitTests.cs:330
-- @test: ScriptOptionsTUnitTests.RecycleCoroutineValidatesFunctionType
-- @compat-notes: Test targets Lua 5.1
function gen() return 'done' end
