-- @lua-versions: 5.1, 5.5
-- @novasharp-only: false
-- @expects-error: false
-- @source: src/tests/WallstopStudios.NovaSharp.Interpreter.Tests.TUnit/Units/Execution/ProcessorExecution/ProcessorCoroutineModuleTUnitTests.cs:428
-- @test: ProcessorCoroutineModuleTUnitTests.ResumeFlattensNestedTupleResultsLua51
-- @compat-notes: Test targets Lua 5.1
function returningTuple()
                    return 'tag', coroutine.running()
                end
