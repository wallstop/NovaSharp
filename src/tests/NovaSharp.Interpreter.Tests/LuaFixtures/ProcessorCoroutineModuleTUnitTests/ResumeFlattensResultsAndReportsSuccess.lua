-- @lua-versions: 5.1+
-- @novasharp-only: false
-- @expects-error: false
-- @source: src/tests/NovaSharp.Interpreter.Tests.TUnit/VM/ProcessorCoroutineModuleTUnitTests.cs:218
-- @test: ProcessorCoroutineModuleTUnitTests.ResumeFlattensResultsAndReportsSuccess
function generator()
                    coroutine.yield('yielded', 42)
                    return 7, 8
                end
