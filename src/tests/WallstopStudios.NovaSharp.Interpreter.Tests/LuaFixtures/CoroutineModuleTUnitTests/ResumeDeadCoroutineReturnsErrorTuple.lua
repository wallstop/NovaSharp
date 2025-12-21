-- @lua-versions: 5.1, 5.5
-- @novasharp-only: false
-- @expects-error: false
-- @source: src/tests/WallstopStudios.NovaSharp.Interpreter.Tests.TUnit/Modules/CoroutineModuleTUnitTests.cs:1177
-- @test: CoroutineModuleTUnitTests.ResumeDeadCoroutineReturnsErrorTuple
-- @compat-notes: Test targets Lua 5.1
function finish()
                    return 'completed'
                end
