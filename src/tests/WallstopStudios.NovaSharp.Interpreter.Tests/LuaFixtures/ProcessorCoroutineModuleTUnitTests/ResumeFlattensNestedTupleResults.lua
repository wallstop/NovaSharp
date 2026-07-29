-- @lua-versions: 5.2, 5.3, 5.4, 5.5
-- @novasharp-only: false
-- @expects-error: false
-- @source: src/tests/WallstopStudios.NovaSharp.Interpreter.Tests.TUnit/Units/Execution/ProcessorExecution/ProcessorCoroutineModuleTUnitTests.cs:861
-- @test: ProcessorCoroutineModuleTUnitTests.ResumeFlattensNestedTupleResults
-- Compatibility notes: Test targets Lua 5.2+
function returningTuple()
                    return 'tag', coroutine.running()
                end
