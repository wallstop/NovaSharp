-- @lua-versions: 5.1, 5.5
-- @novasharp-only: false
-- @expects-error: false
-- @source: src/tests/WallstopStudios.NovaSharp.Interpreter.Tests.TUnit/Modules/CoroutineModuleTUnitTests.cs:875
-- @test: CoroutineModuleTUnitTests.WrapWithPcallReturnsYieldedValues
-- @compat-notes: Test targets Lua 5.1
function buildYieldingWrapper()
                    local step = 0
                    return coroutine.wrap(function()
                        step = step + 1
                        coroutine.yield('first')
                        return 'complete', step
                    end)
                end
