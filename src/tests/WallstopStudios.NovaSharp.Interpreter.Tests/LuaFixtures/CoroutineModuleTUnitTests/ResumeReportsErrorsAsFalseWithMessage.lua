-- @lua-versions: 5.1+
-- @novasharp-only: false
-- @expects-error: false
-- @source: src/tests/NovaSharp.Interpreter.Tests.TUnit/Modules/CoroutineModuleTUnitTests.cs:263
-- @test: CoroutineModuleTUnitTests.ResumeReportsErrorsAsFalseWithMessage
function explode()
                    error('boom', 0)
                end
