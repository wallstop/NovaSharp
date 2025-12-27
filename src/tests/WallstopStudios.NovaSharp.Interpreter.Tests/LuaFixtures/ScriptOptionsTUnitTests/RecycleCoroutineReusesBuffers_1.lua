-- @lua-versions: 5.1
-- @novasharp-only: false
-- @expects-error: false
-- @source: src/tests/WallstopStudios.NovaSharp.Interpreter.Tests.TUnit/Units/Execution/ScriptOptionsTUnitTests.cs:387
-- @test: ScriptOptionsTUnitTests.RecycleCoroutineReusesBuffers
-- @compat-notes: Test targets Lua 5.1
function newGen() return 'recycled' end
