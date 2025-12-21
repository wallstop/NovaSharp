-- @lua-versions: 5.4, 5.5
-- @novasharp-only: false
-- @expects-error: false
-- @source: src/tests/WallstopStudios.NovaSharp.Interpreter.Tests.TUnit/Modules/DebugModuleTUnitTests.cs
-- @test: DebugModuleTUnitTests.GetUserValueLua54ReturnsFalseForNonUserData

-- Test: debug.getuservalue returns (nil, false) for non-userdata in Lua 5.4+
-- Reference: Lua 5.4 manual - getuservalue returns false if userdata doesn't have that value
-- @compat-notes: Second return value indicates whether userdata has that value slot

local val, hasVal = debug.getuservalue("not userdata", 1)
print("val:", tostring(val))
print("hasVal:", tostring(hasVal))
-- For non-userdata, should return nil, false
return val == nil and hasVal == false
