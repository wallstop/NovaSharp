-- @lua-versions: 5.1+
-- @novasharp-only: false
-- @expects-error: true
-- @source: src/tests/WallstopStudios.NovaSharp.Interpreter.Tests.TUnit/Modules/CoroutineModuleTUnitTests.cs:500
-- @test: CoroutineModuleTUnitTests.WrapPropagatesErrorsToCaller
function buildErrorWrapper()
                    return coroutine.wrap(function()
                        error('wrap boom', 0)
                    end)
                end
