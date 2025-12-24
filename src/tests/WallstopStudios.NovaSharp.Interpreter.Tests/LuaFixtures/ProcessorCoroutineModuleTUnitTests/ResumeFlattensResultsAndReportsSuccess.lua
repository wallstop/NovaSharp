-- @lua-versions: 5.1, 5.2, 5.3, 5.4, 5.5
-- @novasharp-only: false
-- @expects-error: false
-- @source: src/tests/WallstopStudios.NovaSharp.Interpreter.Tests.TUnit/Units/Execution/ProcessorExecution/ProcessorCoroutineModuleTUnitTests.cs:351
-- @test: ProcessorCoroutineModuleTUnitTests.ResumeFlattensResultsAndReportsSuccess
-- @compat-notes: Test targets Lua 5.1
function generator()
                    coroutine.yield('yielded', 42)
                    return 7, 8
                end
