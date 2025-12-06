-- @lua-versions: 5.3, 5.4
-- @novasharp-only: false
-- @expects-error: false
-- @source: src/tests/NovaSharp.Interpreter.Tests.TUnit/VM/ScriptExecutionContextTUnitTests.cs:28
-- @test: ScriptExecutionContextTUnitTests.EvaluateSymbolByNameResolvesLocals
-- @compat-notes: Lua 5.3+: bitwise operators
function wrapper()
                    local localValue = 123
                    return assertLocal()
                end
                return wrapper()
