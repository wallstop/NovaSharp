-- @lua-versions: 5.3, 5.4
-- @novasharp-only: false
-- @expects-error: true
-- @source: src/tests/NovaSharp.Interpreter.Tests.TUnit/Modules/CoroutineModuleTUnitTests.cs:551
-- @test: CoroutineModuleTUnitTests.ResumeForceSuspendedCoroutineRejectsArguments
-- @compat-notes: Lua 5.3+: bitwise operators
function heavy()
                    local sum = 0
                    for i = 1, 200 do
                        sum = sum + i
                    end
                    return sum
                end
