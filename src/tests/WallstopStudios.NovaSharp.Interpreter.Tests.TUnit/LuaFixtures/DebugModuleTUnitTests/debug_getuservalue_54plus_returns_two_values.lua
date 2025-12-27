-- @lua-versions: 5.4, 5.5
-- @novasharp-only: false
-- @expects-error: false
-- @source: src/tests/WallstopStudios.NovaSharp.Interpreter.Tests.TUnit/Modules/DebugModuleTUnitTests.cs
-- @test: DebugModuleTUnitTests.GetUserValueLua54ReturnsTwoValuesForUserData

-- Test: debug.getuservalue returns (value, hasValue) tuple in Lua 5.4+
-- Reference: Lua 5.4 manual - debug.getuservalue(u, n) returns n-th user value plus boolean
-- @compat-notes: Lua 5.4 changed getuservalue to return two values

-- This test requires userdata from C; standalone Lua can verify the API shape
-- by calling on a non-userdata and checking return count

local results = {debug.getuservalue("not userdata")}
local count = #results
print("result count:", count)
-- In Lua 5.4+, should return 2 values even for non-userdata
return count == 2
