-- @lua-versions: 5.3, 5.4, 5.5
-- @novasharp-only: false
-- @expects-error: false
-- @source: src\tests\WallstopStudios.NovaSharp.Interpreter.Tests.TUnit\Units\Execution\ScriptExecution\ScriptExecutionContextTUnitTests.cs:28
-- @test: ScriptExecutionContextTUnitTests.EvaluateSymbolByNameResolvesLocals
-- @compat-notes: Lua 5.3+: bitwise operators
function wrapper()
                    local localValue = 123
                    return assertLocal()
                end
                return wrapper()
