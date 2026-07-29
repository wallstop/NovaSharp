-- @lua-versions: 5.1+
-- @novasharp-only: false
-- @expects-error: true
-- @source: src/tests/WallstopStudios.NovaSharp.Interpreter.Tests.TUnit/Units/Execution/ScriptExecution/ScriptOptionsTUnitTests.cs:226
-- @test: ScriptOptionsTUnitTests.IndexNilWithLuaCompatibleErrorsReturnsUpvalueVariableName
local x = nil
                    local function inner()
                        x.foo = 1
                    end
                    inner()
