-- @lua-versions: 5.3, 5.4, 5.5
-- @novasharp-only: false
-- @expects-error: false
-- @source: src\tests\WallstopStudios.NovaSharp.Interpreter.Tests.TUnit\Units\Execution\ScriptExecution\ScriptExecutionContextTUnitTests.cs:118
-- @test: ScriptExecutionContextTUnitTests.GetMetamethodTailCallReturnsTailCallRequest
-- @compat-notes: Lua 5.3+: bitwise operators
local target = {}
                setmetatable(target, { __call = function(_, value) return value end })
                return probeTailCall(target, 7)
