-- @lua-versions: 5.3, 5.4, 5.5
-- @novasharp-only: false
-- @expects-error: false
-- @source: src/tests/WallstopStudios.NovaSharp.Interpreter.Tests.TUnit/Modules/CoroutineModuleTUnitTests.cs:645
-- @test: CoroutineModuleTUnitTests.WrapWithPcallReturnsYieldedValues
-- @compat-notes: Lua 5.3+: bitwise operators
function buildYieldingWrapper()
                    local step = 0
                    return coroutine.wrap(function()
                        step = step + 1
                        coroutine.yield('first')
                        return 'complete', step
                    end)
                end
