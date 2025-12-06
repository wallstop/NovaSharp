-- @lua-versions: 5.3, 5.4
-- @novasharp-only: false
-- @expects-error: false
-- @source: src/tests/NovaSharp.Interpreter.Tests.TUnit/VM/ProcessorCoroutineModuleTUnitTests.cs:39
-- @test: ProcessorCoroutineModuleTUnitTests.RunningInsideCoroutineReturnsFalse
-- @compat-notes: Lua 5.3+: bitwise operators
function runningCheck()
                    local _, isMain = coroutine.running()
                    return isMain
                end
