-- @lua-versions: 5.1
-- @novasharp-only: false
-- @expects-error: true
-- @source: src\tests\WallstopStudios.NovaSharp.Interpreter.Tests.TUnit\Units\Execution\ScriptOptionsTUnitTests.cs:382
-- @test: ScriptOptionsTUnitTests.RecycleCoroutineReusesBuffers
-- Test targets Lua 5.1
function gen() return 'original' end
