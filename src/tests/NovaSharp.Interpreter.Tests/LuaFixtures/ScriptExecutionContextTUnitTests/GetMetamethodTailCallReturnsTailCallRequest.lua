-- @lua-versions: 5.3, 5.4
-- @novasharp-only: false
-- @expects-error: false
-- @source: src/tests/NovaSharp.Interpreter.Tests.TUnit/VM/ScriptExecutionContextTUnitTests.cs:118
-- @test: ScriptExecutionContextTUnitTests.GetMetamethodTailCallReturnsTailCallRequest
-- @compat-notes: Lua 5.3+: bitwise operators
local target = {}
                setmetatable(target, { __call = function(_, value) return value end })
                return probeTailCall(target, 7)
