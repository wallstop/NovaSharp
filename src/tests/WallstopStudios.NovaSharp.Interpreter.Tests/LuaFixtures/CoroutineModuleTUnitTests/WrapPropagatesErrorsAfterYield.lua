-- @lua-versions: 5.1, 5.5
-- @novasharp-only: false
-- @expects-error: true
-- @source: src/tests/WallstopStudios.NovaSharp.Interpreter.Tests.TUnit/Modules/CoroutineModuleTUnitTests.cs:721
-- @test: CoroutineModuleTUnitTests.WrapPropagatesErrorsAfterYield
-- @compat-notes: Test targets Lua 5.1
function buildDelayedErrorWrapper()
                    return coroutine.wrap(function()
                        coroutine.yield('first')
                        error('wrap later', 0)
                    end)
                end
