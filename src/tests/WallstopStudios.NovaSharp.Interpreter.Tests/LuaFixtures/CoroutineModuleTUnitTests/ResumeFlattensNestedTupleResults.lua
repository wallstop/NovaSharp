-- @lua-versions: 5.1+
-- @novasharp-only: false
-- @expects-error: true
-- @source: src/tests/WallstopStudios.NovaSharp.Interpreter.Tests.TUnit/Modules/CoroutineModuleTUnitTests.cs:300
-- @test: CoroutineModuleTUnitTests.ResumeFlattensNestedTupleResults
function returningTuple()
                    return 'tag', coroutine.running()
                end
