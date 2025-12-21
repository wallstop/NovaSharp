-- @lua-versions: 5.2, 5.3, 5.4, 5.5
-- @novasharp-only: false
-- @expects-error: true
-- @source: src/tests/WallstopStudios.NovaSharp.Interpreter.Tests.TUnit/Modules/CoroutineModuleTUnitTests.cs:1136
-- @test: CoroutineModuleTUnitTests.ResumeFromDifferentThreadThrowsInvalidOperation
-- @compat-notes: Test targets Lua 5.2+
function pause()
                    waitForSignal()
                    return 'done'
                end
