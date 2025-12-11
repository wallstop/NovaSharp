-- @lua-versions: 5.1+
-- @novasharp-only: false
-- @expects-error: true
-- @source: src\tests\WallstopStudios.NovaSharp.Interpreter.Tests.TUnit\Modules\CoroutineModuleTUnitTests.cs:899
-- @test: CoroutineModuleTUnitTests.ResumeDeadCoroutineReturnsErrorTuple
function finish()
                    return 'completed'
                end
