-- @lua-versions: 5.3, 5.4, 5.5
-- @novasharp-only: false
-- @expects-error: false
-- @source: src\tests\WallstopStudios.NovaSharp.Interpreter.Tests.TUnit\Modules\CoroutineModuleTUnitTests.cs:63
-- @test: CoroutineModuleTUnitTests.StatusReflectsLifecycleAndForceSuspendedStates
-- @compat-notes: Lua 5.3+: bitwise operators
function compute()
                    local sum = 0
                    for i = 1, 200 do
                        sum = sum + i
                    end
                    return sum
                end
