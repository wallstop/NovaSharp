-- @lua-versions: 5.1+
-- @novasharp-only: false
-- @expects-error: false
-- @source: src/tests/NovaSharp.Interpreter.Tests.TUnit/VM/ScriptExecutionContextTUnitTests.cs:60
-- @test: ScriptExecutionContextTUnitTests.CurrentGlobalEnvExposesGlobals
function trigger()
                    return probeEnv()
                end
                return trigger()
