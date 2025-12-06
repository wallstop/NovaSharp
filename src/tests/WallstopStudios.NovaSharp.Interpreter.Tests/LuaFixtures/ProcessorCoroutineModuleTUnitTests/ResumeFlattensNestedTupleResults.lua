-- @lua-versions: 5.1+
-- @novasharp-only: false
-- @expects-error: false
-- @source: src/tests/NovaSharp.Interpreter.Tests.TUnit/VM/ProcessorCoroutineModuleTUnitTests.cs:280
-- @test: ProcessorCoroutineModuleTUnitTests.ResumeFlattensNestedTupleResults
function returningTuple()
                    return 'tag', coroutine.running()
                end
