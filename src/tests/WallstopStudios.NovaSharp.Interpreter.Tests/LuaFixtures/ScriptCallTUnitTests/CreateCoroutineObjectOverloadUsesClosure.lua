-- @lua-versions: 5.1+
-- @novasharp-only: false
-- @expects-error: false
-- @source: src/tests/WallstopStudios.NovaSharp.Interpreter.Tests.TUnit/Units/Execution/ScriptExecution/ScriptCallTUnitTests.cs:345
-- @test: ScriptCallTUnitTests.CreateCoroutineObjectOverloadUsesClosure
function generator()
                    coroutine.yield(5)
                    return 6
                end
