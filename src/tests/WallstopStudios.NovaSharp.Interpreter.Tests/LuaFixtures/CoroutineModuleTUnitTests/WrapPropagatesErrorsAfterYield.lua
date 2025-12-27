-- @lua-versions: 5.1+
-- @novasharp-only: false
-- @expects-error: true
-- @source: src/tests/WallstopStudios.NovaSharp.Interpreter.Tests.TUnit/Modules/CoroutineModuleTUnitTests.cs:645
-- @test: CoroutineModuleTUnitTests.WrapPropagatesErrorsAfterYield
function buildDelayedErrorWrapper()
                    return coroutine.wrap(function()
                        coroutine.yield('first')
                        error('wrap later', 0)
                    end)
                end
