-- @lua-versions: 5.1, 5.2, 5.3, 5.4, 5.5
-- @novasharp-only: false
-- @expects-error: false
-- @source: src/tests/WallstopStudios.NovaSharp.Interpreter.Tests.TUnit/Units/Execution/ScriptExecution/ScriptCallTUnitTests.cs:475
-- @test: ScriptCallTUnitTests.CreateCoroutineObjectOverloadUsesClosure
-- @compat-notes: Test targets Lua 5.1
function generator()
                    coroutine.yield(5)
                    return 6
                end
