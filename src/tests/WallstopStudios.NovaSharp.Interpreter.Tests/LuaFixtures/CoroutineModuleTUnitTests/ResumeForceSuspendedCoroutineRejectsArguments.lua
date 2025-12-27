-- @lua-versions: 5.1
-- @novasharp-only: false
-- @expects-error: false
-- @source: src/tests/WallstopStudios.NovaSharp.Interpreter.Tests.TUnit/Modules/CoroutineModuleTUnitTests.cs:684
-- @test: CoroutineModuleTUnitTests.ResumeForceSuspendedCoroutineRejectsArguments
-- @compat-notes: Test targets Lua 5.1
function heavy()
                    local sum = 0
                    for i = 1, 200 do
                        sum = sum + i
                    end
                    return sum
                end
