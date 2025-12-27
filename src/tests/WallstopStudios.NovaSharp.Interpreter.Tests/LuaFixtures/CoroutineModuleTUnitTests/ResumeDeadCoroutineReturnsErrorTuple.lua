-- @lua-versions: 5.1+
-- @novasharp-only: false
-- @expects-error: false
-- @source: src/tests/WallstopStudios.NovaSharp.Interpreter.Tests.TUnit/Modules/CoroutineModuleTUnitTests.cs:1296
-- @test: CoroutineModuleTUnitTests.ResumeDeadCoroutineReturnsErrorTuple
function finish()
                    return 'completed'
                end
