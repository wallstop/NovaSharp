-- @lua-versions: 5.3, 5.4, 5.5
-- @novasharp-only: false
-- @expects-error: false
-- @source: src\tests\WallstopStudios.NovaSharp.Interpreter.Tests.TUnit\Units\Execution\ScriptExecution\ScriptExecutionContextTUnitTests.cs:88
-- @test: ScriptExecutionContextTUnitTests.GetMetatableReturnsAssignedMetatable
-- @compat-notes: Lua 5.3+: bitwise operators
local t = {}
                setmetatable(t, { marker = 42 })
                return probeMeta(t)
