-- @lua-versions: 5.1, 5.2, 5.3
-- @novasharp-only: false
-- @expects-error: false
-- @source: src/tests/WallstopStudios.NovaSharp.Interpreter.Tests.TUnit/Modules/DebugModuleTUnitTests.cs
-- @test: DebugModuleTUnitTests.GetUserValueLua53ReturnsOnlyOneValue

-- Test: debug.getuservalue returns only one value in Lua 5.1-5.3
-- Reference: Lua 5.3 manual - debug.getuservalue(u) returns single uservalue
-- @compat-notes: Pre-5.4, only single return value

local results = {debug.getuservalue("not userdata")}
local count = #results
print("result count:", count)
-- In Lua 5.1-5.3, getuservalue returns only 1 value (nil for non-userdata)
return count == 1
