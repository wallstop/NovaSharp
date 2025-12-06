-- @lua-versions: 5.1+
-- @novasharp-only: false
-- @expects-error: false
-- @source: src/tests/NovaSharp.Interpreter.Tests.TUnit/VM/ProcessorCoroutineModuleTUnitTests.cs:247
-- @test: ProcessorCoroutineModuleTUnitTests.ResumeReportsErrorsAsFalseWithMessage
function explode()
                    error('boom', 0)
                end
