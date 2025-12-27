-- @lua-versions: 5.2, 5.3, 5.4, 5.5
-- @novasharp-only: false
-- @expects-error: false
-- @source: src/tests/WallstopStudios.NovaSharp.Interpreter.Tests.TUnit/Modules/IoModuleTUnitTests.cs
-- @test: IoModuleTUnitTests.OpenThrowsForInvalidModeCharacterLua52Plus

-- Test: io.open throws error for invalid mode character ('x') in Lua 5.2+
-- Reference: Lua 5.2+ manual - io.open
-- @compat-notes: Lua 5.1 returns (nil, error); Lua 5.2+ throws error

local ok, err = pcall(function()
    return io.open("/tmp/test_file.txt", "x")
end)
print("throws error:", not ok)
print("error contains 'invalid mode':", (err or ""):find("invalid mode") ~= nil)
return not ok
