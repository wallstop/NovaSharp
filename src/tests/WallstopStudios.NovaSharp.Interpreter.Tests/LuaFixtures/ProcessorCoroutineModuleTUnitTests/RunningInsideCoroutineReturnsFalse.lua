-- @lua-versions: 5.2, 5.3, 5.4, 5.5
-- @novasharp-only: false
-- @expects-error: false
-- @source: src/tests/WallstopStudios.NovaSharp.Interpreter.Tests.TUnit/Units/Execution/ProcessorExecution/ProcessorCoroutineModuleTUnitTests.cs:106
-- @test: ProcessorCoroutineModuleTUnitTests.RunningInsideCoroutineReturnsFalse
-- @compat-notes: Lua 5.2+: coroutine.running() inside a coroutine returns (thread, false)

-- Test: In Lua 5.2+, coroutine.running() inside a coroutine returns (thread, false)
-- Reference: Lua 5.2+ manual §6.2

function runningCheck()
    local _, isMain = coroutine.running()
    return isMain
end

local co = coroutine.create(runningCheck)
local ok, result = coroutine.resume(co)

assert(ok, "Resume should succeed")
assert(result == false, "Expected isMain=false inside coroutine in Lua 5.2+, got " .. tostring(result))
print("PASS: coroutine.running() inside coroutine returns (thread, false) in Lua 5.2+")
