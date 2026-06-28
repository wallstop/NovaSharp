-- Test: setfenv should not exist in Lua 5.2+
-- Expected: error
-- Description: setfenv was removed in Lua 5.2

-- setfenv should be nil in 5.2+
-- @lua-versions: 5.2
-- @novasharp-only: false
-- @expects-error: true
-- @source: src/tests/WallstopStudios.NovaSharp.Interpreter.Tests.TUnit/Modules/SetFenvGetFenvTUnitTests.cs
-- @test: SetFenvGetFenvTUnitTests.setfenv_not_available_52plus
assert(setfenv == nil, "setfenv should be nil in Lua 5.2+")
print("PASS")
