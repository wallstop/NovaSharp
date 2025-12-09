-- @lua-versions: 5.1+
-- @novasharp-only: false
-- @expects-error: true
-- @source: src\tests\WallstopStudios.NovaSharp.Interpreter.Tests.TUnit\Units\Execution\ScriptOptionsTUnitTests.cs:227
-- @test: ScriptOptionsTUnitTests.RecycleCoroutineValidatesCoroutineState
function gen() coroutine.yield(1) return 2 end
