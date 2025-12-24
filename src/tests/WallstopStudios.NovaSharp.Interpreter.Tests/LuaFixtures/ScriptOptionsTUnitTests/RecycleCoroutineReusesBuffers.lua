-- @lua-versions: 5.1, 5.2, 5.3, 5.4, 5.5
-- @novasharp-only: false
-- @expects-error: true
-- @source: src/tests/WallstopStudios.NovaSharp.Interpreter.Tests.TUnit/Units/Execution/ScriptOptionsTUnitTests.cs:382
-- @test: ScriptOptionsTUnitTests.RecycleCoroutineReusesBuffers
-- @compat-notes: Test targets Lua 5.1
function gen() return 'original' end
