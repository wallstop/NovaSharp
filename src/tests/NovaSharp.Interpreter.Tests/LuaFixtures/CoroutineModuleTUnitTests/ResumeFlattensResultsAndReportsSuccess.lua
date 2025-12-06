-- @lua-versions: 5.1+
-- @novasharp-only: false
-- @expects-error: false
-- @source: src/tests/NovaSharp.Interpreter.Tests.TUnit/Modules/CoroutineModuleTUnitTests.cs:234
-- @test: CoroutineModuleTUnitTests.ResumeFlattensResultsAndReportsSuccess
function generator()
                    coroutine.yield('yielded', 42)
                    return 7, 8
                end
