-- @lua-versions: 5.2, 5.3, 5.4, 5.5
-- @novasharp-only: false
-- @expects-error: false
-- @source: src/tests/WallstopStudios.NovaSharp.Interpreter.Tests.TUnit/Units/Execution/ProcessorExecution/ProcessorCoroutineModuleTUnitTests.cs:45
-- @test: ProcessorCoroutineModuleTUnitTests.RunningFromMainReturnsMainCoroutine
-- @compat-notes: Lua 5.2+: coroutine.running() returns (thread, isMain) tuple

-- Test: In Lua 5.2+, coroutine.running() returns (thread, true) from main thread
-- Reference: Lua 5.2+ manual §6.2

local co, isMain = coroutine.running()

-- In Lua 5.2+, this should return a thread and isMain=true from main thread
assert(type(co) == "thread", "Expected thread from coroutine.running() in Lua 5.2+")
assert(isMain == true, "Expected isMain=true from coroutine.running() on main thread in Lua 5.2+")
print("PASS: coroutine.running() returns (thread, true) from main in Lua 5.2+")
return co, isMain
