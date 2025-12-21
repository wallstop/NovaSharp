-- @lua-versions: 5.1, 5.5
-- @novasharp-only: false
-- @expects-error: false
-- @source: src/tests/WallstopStudios.NovaSharp.Interpreter.Tests.TUnit/Units/Execution/ProcessorExecution/ProcessorCoroutineModuleTUnitTests.cs:385
-- @test: ProcessorCoroutineModuleTUnitTests.ResumeReportsErrorsAsFalseWithMessage
-- @compat-notes: Test targets Lua 5.1
function explode()
                    error('boom', 0)
                end
