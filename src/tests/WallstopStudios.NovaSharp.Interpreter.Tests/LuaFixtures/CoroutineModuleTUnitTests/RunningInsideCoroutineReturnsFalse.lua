-- @lua-versions: 5.3, 5.4
-- @novasharp-only: false
-- @expects-error: false
-- @source: src/tests/NovaSharp.Interpreter.Tests.TUnit/Modules/CoroutineModuleTUnitTests.cs:43
-- @test: CoroutineModuleTUnitTests.RunningInsideCoroutineReturnsFalse
-- @compat-notes: Lua 5.3+: bitwise operators
function runningCheck()
                    local _, isMain = coroutine.running()
                    return isMain
                end
