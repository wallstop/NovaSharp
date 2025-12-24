-- @lua-versions: 5.1, 5.2, 5.3, 5.4, 5.5
-- @novasharp-only: false
-- @expects-error: false
-- @source: src/tests/WallstopStudios.NovaSharp.Interpreter.Tests.TUnit/Modules/CoroutineModuleTUnitTests.cs:314
-- @test: CoroutineModuleTUnitTests.ResumeFlattensResultsAndReportsSuccess
-- @compat-notes: Test targets Lua 5.1
function generator()
                    coroutine.yield('yielded', 42)
                    return 7, 8
                end
