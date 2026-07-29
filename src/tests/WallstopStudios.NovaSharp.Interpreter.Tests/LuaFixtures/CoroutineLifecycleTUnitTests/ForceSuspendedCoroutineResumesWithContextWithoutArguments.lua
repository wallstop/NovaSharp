-- @lua-versions: 5.1
-- @novasharp-only: false
-- @expects-error: false
-- @source: src/tests/WallstopStudios.NovaSharp.Interpreter.Tests.TUnit/Units/DataTypes/CoroutineLifecycleTUnitTests.cs:208
-- @test: CoroutineLifecycleTUnitTests.ForceSuspendedCoroutineResumesWithContextWithoutArguments
-- Test targets Lua 5.1
function heavyweight()
                    local total = 0
                    for i = 1, 300 do
                        total = total + i
                    end
                    return total
                end
