-- @lua-versions: 5.1+
-- @novasharp-only: false
-- @expects-error: false
-- @source: src/tests/NovaSharp.Interpreter.Tests.TUnit/VM/ScriptCallTUnitTests.cs:345
-- @test: ScriptCallTUnitTests.CreateCoroutineObjectOverloadUsesClosure
function generator()
                    coroutine.yield(5)
                    return 6
                end
