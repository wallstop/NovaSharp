-- @lua-versions: 5.1+
-- @novasharp-only: false
-- @expects-error: false
-- @source: src/tests/WallstopStudios.NovaSharp.Interpreter.Tests.TUnit/Modules/CoroutineModuleTUnitTests.cs:498
-- @test: CoroutineModuleTUnitTests.ResumeFlattensTrailingTupleResults
function invokeNestedBuilder()
                    return buildNestedResult()
                end
