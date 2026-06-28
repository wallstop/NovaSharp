-- Test: Basic getfenv functionality
-- Expected: success
-- Description: Tests getfenv() returns _G by default

-- Store reference for comparison
-- @lua-versions: 5.1
-- @novasharp-only: false
-- @expects-error: false
-- @source: src/tests/WallstopStudios.NovaSharp.Interpreter.Tests.TUnit/Modules/SetFenvGetFenvTUnitTests.cs
-- @test: SetFenvGetFenvTUnitTests.getfenv_basic_51
local _G_ref = _G

-- getfenv() with no argument returns environment of calling function
local env = getfenv()
assert(env == _G_ref, "getfenv() should return _G")

-- getfenv(0) returns global environment
local env0 = getfenv(0)
assert(env0 == _G_ref, "getfenv(0) should return _G")

-- getfenv(1) returns current function's environment
local env1 = getfenv(1)
assert(env1 == _G_ref, "getfenv(1) should return _G")

print("PASS")
