-- @lua-versions: 5.1+
-- @novasharp-only: false
-- @expects-error: false
-- @source: src/tests/WallstopStudios.NovaSharp.Interpreter.Tests.TUnit/Units/Execution/ScriptExecution/ScriptExecutionContextTUnitTests.cs:31
-- @test: ScriptExecutionContextTUnitTests.EvaluateSymbolByNameResolvesLocals
function wrapper()
                    local localValue = 123
                    return assertLocal()
                end
                return wrapper()
