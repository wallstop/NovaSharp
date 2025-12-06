-- @lua-versions: 5.1+
-- @novasharp-only: false
-- @expects-error: false
-- @source: src/tests/NovaSharp.Interpreter.Tests.TUnit/Modules/CoroutineModuleTUnitTests.cs:411
-- @test: CoroutineModuleTUnitTests.ResumeFlattensTrailingTupleResults
function invokeNestedBuilder()
                    return buildNestedResult()
                end
