-- @lua-versions: 5.1+
-- @novasharp-only: false
-- @expects-error: false
-- @source: src/tests/WallstopStudios.NovaSharp.Interpreter.Tests.TUnit/Units/Execution/ScriptExecution/ScriptExecutionContextTUnitTests.cs:93
-- @test: ScriptExecutionContextTUnitTests.GetMetatableReturnsAssignedMetatable
local t = {}
                setmetatable(t, { marker = 42 })
                return probeMeta(t)
