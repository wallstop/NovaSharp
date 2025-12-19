-- @lua-versions: 5.1
-- @novasharp-only: false
-- @expects-error: false
-- @source: src/tests/WallstopStudios.NovaSharp.Interpreter.Tests.TUnit/Units/Execution/ProcessorExecution/ProcessorCoroutineModuleTUnitTests.cs:72
-- @test: ProcessorCoroutineModuleTUnitTests.RunningInsideCoroutineReturnsCoroutineInLua51
-- @compat-notes: Lua 5.1: coroutine.running() inside a coroutine returns only the coroutine (single value)

-- Test: In Lua 5.1, coroutine.running() inside a coroutine returns only the thread
-- Reference: Lua 5.1 manual §5.2

function runningCheck()
    local co = coroutine.running()
    return type(co), co
end

local co = coroutine.create(runningCheck)
local ok, t, result = coroutine.resume(co)

assert(ok, "Resume should succeed")
assert(t == "thread", "Expected 'thread' type, got " .. tostring(t))
assert(type(result) == "thread", "Expected thread result, got " .. type(result))
print("PASS: coroutine.running() inside coroutine returns thread in Lua 5.1")
