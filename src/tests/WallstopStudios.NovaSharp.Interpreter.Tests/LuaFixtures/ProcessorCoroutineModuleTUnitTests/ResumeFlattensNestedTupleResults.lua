-- @lua-versions: 5.2+
-- @novasharp-only: false
-- @expects-error: false
-- @source: src\tests\WallstopStudios.NovaSharp.Interpreter.Tests.TUnit\Units\Execution\ProcessorExecution\ProcessorCoroutineModuleTUnitTests.cs:469
-- @test: ProcessorCoroutineModuleTUnitTests.ResumeFlattensNestedTupleResults
-- Test targets Lua 5.2+
function returningTuple()
                    return 'tag', coroutine.running()
                end
