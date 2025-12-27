-- @lua-versions: 5.1
-- @novasharp-only: false
-- @expects-error: false
-- @source: src/tests/WallstopStudios.NovaSharp.Interpreter.Tests.TUnit/Units/Execution/ProcessorExecution/ProcessorCoroutineModuleTUnitTests.cs:26
-- @test: ProcessorCoroutineModuleTUnitTests.RunningFromMainReturnsNilInLua51
-- @compat-notes: Lua 5.1: coroutine.running() returns nil from main thread

-- Test: In Lua 5.1, coroutine.running() returns nil when called from the main thread
-- Reference: Lua 5.1 manual §5.2

local result = coroutine.running()

-- In Lua 5.1, this should be nil when called from main thread
assert(result == nil, "Expected nil from coroutine.running() on main thread in Lua 5.1")
print("PASS: coroutine.running() returns nil from main in Lua 5.1")
return result
