-- @lua-versions: 5.1, 5.2, 5.3
-- @novasharp-only: false
-- @expects-error: false
-- @source: src/tests/WallstopStudios.NovaSharp.Interpreter.Tests.TUnit/Units/Execution/ProcessorExecution/ProcessorCoroutineCloseTUnitTests.cs:430
-- @test: ProcessorCoroutineCloseTUnitTests.CoroutineCloseIsNilInPreLua54
-- @compat-notes: coroutine.close() is a Lua 5.4+ function and should be nil in earlier versions

-- Test: coroutine.close should be nil in Lua 5.1-5.3
-- Reference: Lua 5.4 manual §6.2 (coroutine.close added in 5.4)

local closeFunc = coroutine.close

assert(closeFunc == nil, "coroutine.close should be nil in pre-5.4, got " .. type(closeFunc))
print("PASS: coroutine.close is nil in pre-5.4")
