-- @lua-versions: 5.1+
-- @novasharp-only: false
-- @expects-error: true
-- @source: src/tests/WallstopStudios.NovaSharp.Interpreter.Tests.TUnit/Units/DataTypes/CoroutineLifecycleTUnitTests.cs:105
-- @test: CoroutineLifecycleTUnitTests.RecycleCoroutineThrowsWhenNotDead
function sample() coroutine.yield(1) end
