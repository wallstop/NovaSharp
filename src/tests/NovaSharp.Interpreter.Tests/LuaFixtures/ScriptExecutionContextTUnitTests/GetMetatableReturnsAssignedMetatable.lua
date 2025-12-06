-- @lua-versions: 5.3, 5.4
-- @novasharp-only: false
-- @expects-error: false
-- @source: src/tests/NovaSharp.Interpreter.Tests.TUnit/VM/ScriptExecutionContextTUnitTests.cs:88
-- @test: ScriptExecutionContextTUnitTests.GetMetatableReturnsAssignedMetatable
-- @compat-notes: Lua 5.3+: bitwise operators
local t = {}
                setmetatable(t, { marker = 42 })
                return probeMeta(t)
