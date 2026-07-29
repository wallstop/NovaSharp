-- @lua-versions: 5.1+
-- @novasharp-only: false
-- @expects-error: false
-- @source: src/tests/WallstopStudios.NovaSharp.Interpreter.Tests.TUnit/Modules/CoroutineModuleTUnitTests.cs:540
-- @test: CoroutineModuleTUnitTests.WrapZeroValueYieldReturnsNoValues
local wrapped = coroutine.wrap(function()
                    coroutine.yield()
                    return 'done'
                end)

                local yieldCount = select('#', wrapped())
                local finalCount = select('#', wrapped())
                return yieldCount, finalCount
