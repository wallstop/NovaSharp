-- @lua-versions: 5.4, 5.5
-- @novasharp-only: false
-- @expects-error: false
-- @source: src/tests/WallstopStudios.NovaSharp.Interpreter.Tests.TUnit/Units/Execution/ProcessorExecution/ProcessorCoroutineCloseTUnitTests.cs:443
-- @test: ProcessorCoroutineCloseTUnitTests.CoroutineCloseIsAvailableInLua54Plus
-- @compat-notes: coroutine.close() is available starting from Lua 5.4

-- Test: coroutine.close should be available in Lua 5.4+
-- Reference: Lua 5.4 manual §6.2

local closeFunc = coroutine.close

assert(type(closeFunc) == "function", "coroutine.close should be a function in 5.4+, got " .. type(closeFunc))
print("PASS: coroutine.close is available in Lua 5.4+")
