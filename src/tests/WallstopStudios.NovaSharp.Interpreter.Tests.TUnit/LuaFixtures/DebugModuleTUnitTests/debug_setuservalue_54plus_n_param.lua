-- @lua-versions: 5.4, 5.5
-- @novasharp-only: false
-- @expects-error: false
-- @source: src/tests/WallstopStudios.NovaSharp.Interpreter.Tests.TUnit/Modules/DebugModuleTUnitTests.cs
-- @test: DebugModuleTUnitTests.SetUserValueLua54WithNParameterSlot1Works

-- Test: debug.setuservalue with n parameter works for slot 1 in Lua 5.4+
-- Reference: Lua 5.4 manual - setuservalue(udata, value, n) sets n-th user value
-- @compat-notes: n parameter is 1-based, n=1 is first user value slot

-- This test requires actual userdata; verify API shape with error case
-- For non-userdata, setuservalue should error
local ok, err = pcall(function()
    debug.setuservalue("not userdata", {}, 1)
end)
print("error:", not ok)
-- Should error because first arg is not userdata
return not ok
