-- @lua-versions: 5.1+
-- @novasharp-only: false
-- @expects-error: false
-- @source: src/tests/WallstopStudios.NovaSharp.Interpreter.Tests.TUnit/Units/Execution/ScriptExecution/ScriptExecutionContextTUnitTests.cs:148
-- @test: ScriptExecutionContextTUnitTests.PerformMessageDecorationDecoratesException
function decorator(message)
                    return 'decorated:' .. message
                end
