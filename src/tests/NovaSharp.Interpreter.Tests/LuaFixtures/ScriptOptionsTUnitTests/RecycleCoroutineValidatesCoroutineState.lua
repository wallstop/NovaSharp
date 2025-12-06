-- @lua-versions: 5.1+
-- @novasharp-only: false
-- @expects-error: true
-- @source: src/tests/NovaSharp.Interpreter.Tests.TUnit/Units/ScriptOptionsTUnitTests.cs:226
-- @test: ScriptOptionsTUnitTests.RecycleCoroutineValidatesCoroutineState
function gen() coroutine.yield(1) return 2 end
