-- @lua-versions: 5.1
-- @novasharp-only: false
-- @expects-error: false
-- @source: src/tests/WallstopStudios.NovaSharp.Interpreter.Tests.TUnit/Units/DataTypes/CoroutineLifecycleTUnitTests.cs:105
-- @test: CoroutineLifecycleTUnitTests.RecycleCoroutineThrowsWhenNotDead
-- Test targets Lua 5.1
function sample() coroutine.yield(1) end
