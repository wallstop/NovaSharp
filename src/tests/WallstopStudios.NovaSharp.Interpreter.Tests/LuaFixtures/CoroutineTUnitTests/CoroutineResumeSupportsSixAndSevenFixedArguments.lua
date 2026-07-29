-- @lua-versions: 5.1+
-- @novasharp-only: false
-- @expects-error: false
-- @source: src/tests/WallstopStudios.NovaSharp.Interpreter.Tests.TUnit/EndToEnd/CoroutineTUnitTests.cs:220
-- @test: CoroutineTUnitTests.CoroutineResumeSupportsSixAndSevenFixedArguments
return function(...) return coroutine.yield(select('#', ...), select(select('#', ...), ...)) end
