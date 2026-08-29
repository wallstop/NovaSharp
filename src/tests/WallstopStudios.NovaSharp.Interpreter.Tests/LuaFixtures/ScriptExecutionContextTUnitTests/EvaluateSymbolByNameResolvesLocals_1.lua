-- @lua-versions: novasharp-only
-- @novasharp-only: true
-- @expects-error: false
-- @source: src/tests/WallstopStudios.NovaSharp.Interpreter.Tests.TUnit/Units/Execution/ScriptExecution/ScriptExecutionContextTUnitTests.cs:33
-- @test: ScriptExecutionContextTUnitTests.EvaluateSymbolByNameResolvesLocals
-- Uses injected callback: assertLocal
function wrapper()
                    local localValue = 123
                    local activeNilShadowsOuter
                    do
                        local localValue = nil
                        activeNilShadowsOuter = assertLocal() == nil
                    end
                    return activeNilShadowsOuter, assertLocal()
                end
                return wrapper()
