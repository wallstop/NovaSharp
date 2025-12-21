-- @lua-versions: 5.1, 5.5
-- @novasharp-only: false
-- @expects-error: false
-- @source: src/tests/WallstopStudios.NovaSharp.Interpreter.Tests.TUnit/Modules/CoroutineModuleTUnitTests.cs:348
-- @test: CoroutineModuleTUnitTests.ResumeReportsErrorsAsFalseWithMessage
-- @compat-notes: Test targets Lua 5.1
function explode()
                    error('boom', 0)
                end
