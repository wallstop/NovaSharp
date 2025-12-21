-- @lua-versions: 5.1, 5.5
-- @novasharp-only: false
-- @expects-error: true
-- @source: src/tests/WallstopStudios.NovaSharp.Interpreter.Tests.TUnit/Units/Execution/ScriptOptionsTUnitTests.cs:304
-- @test: ScriptOptionsTUnitTests.RecycleCoroutineValidatesCoroutineState
-- @compat-notes: Test targets Lua 5.1
function gen() coroutine.yield(1) return 2 end
