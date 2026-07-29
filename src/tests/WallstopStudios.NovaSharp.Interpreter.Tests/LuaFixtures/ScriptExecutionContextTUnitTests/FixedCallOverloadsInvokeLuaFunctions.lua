-- @lua-versions: 5.1+
-- @novasharp-only: false
-- @expects-error: false
-- @source: src/tests/WallstopStudios.NovaSharp.Interpreter.Tests.TUnit/Units/Execution/ScriptExecution/ScriptExecutionContextTUnitTests.cs:278
-- @test: ScriptExecutionContextTUnitTests.FixedCallOverloadsInvokeLuaFunctions
return function(...)
                        local total = 0
                        for i = 1, select('#', ...) do
                            total = total + select(i, ...)
                        end
                        return total
                    end
