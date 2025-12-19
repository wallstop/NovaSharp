-- @lua-versions: 5.1
-- @novasharp-only: false
-- @expects-error: false
-- @source: src/tests/WallstopStudios.NovaSharp.Interpreter.Tests.TUnit/Units/Execution/ProcessorExecution/ProcessorCoroutineModuleTUnitTests.cs:474
-- @test: ProcessorCoroutineModuleTUnitTests.ResumeFlattensNestedTupleResultsLua51
-- @compat-notes: Lua 5.1: coroutine.running() returns only 1 value, so resume returns 3 values

-- Test: In Lua 5.1, resume flattens tuple results; coroutine.running() returns 1 value
-- Reference: Lua 5.1 manual §5.2

function returningTuple()
    return 'tag', coroutine.running()
end

local co = coroutine.create(returningTuple)
local ok, tag, thread = coroutine.resume(co)

-- In Lua 5.1: resume returns (true, 'tag', thread) = 3 elements
assert(ok == true, "Resume should succeed")
assert(tag == 'tag', "Expected 'tag', got " .. tostring(tag))
assert(type(thread) == "thread", "Expected thread, got " .. type(thread))
print("PASS: resume flattens tuple results in Lua 5.1 (3 values)")
