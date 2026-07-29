-- @lua-versions: all
-- @novasharp-only: false
-- @expects-error: false
-- @source: src/tests/WallstopStudios.NovaSharp.Interpreter.Tests.TUnit/Units/Execution/VmStackCeilingTUnitTests.cs:65
-- @test: VmStackCeilingTUnitTests.StackOverflowIsCatchableViaPcall

-- Runaway non-tail recursion must surface as a catchable "stack overflow" error rather than crashing
-- the host. The overflow depth is an implementation detail that differs between reference Lua versions,
-- so this fixture asserts only the version-stable contract: pcall returns false with a string error whose
-- text contains "stack overflow".
local function f()
    return 1 + f()
end

local ok, err = pcall(f)
assert(ok == false, "expected pcall to fail on runaway recursion")
assert(type(err) == "string", "expected string error, got " .. type(err))
assert(err:find("stack overflow") ~= nil, "expected 'stack overflow', got " .. tostring(err))
print("PASS")
