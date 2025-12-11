-- @lua-versions: 5.1+
-- @novasharp-only: false
-- @expects-error: false
-- @source: src\tests\WallstopStudios.NovaSharp.Interpreter.Tests.TUnit\Modules\CoroutineModuleTUnitTests.cs:618
-- @test: CoroutineModuleTUnitTests.WrapWithPcallCapturesErrors
function buildPcallWrapper()
                    return coroutine.wrap(function()
                        error('wrapped failure', 0)
                    end)
                end
