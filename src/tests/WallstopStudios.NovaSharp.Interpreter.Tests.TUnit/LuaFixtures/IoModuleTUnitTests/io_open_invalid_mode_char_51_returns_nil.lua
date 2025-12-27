-- @lua-versions: 5.1
-- @novasharp-only: false
-- @expects-error: false
-- @source: src/tests/WallstopStudios.NovaSharp.Interpreter.Tests.TUnit/Modules/IoModuleTUnitTests.cs
-- @test: IoModuleTUnitTests.OpenReturnsNilForInvalidModeCharacterLua51

-- Test: io.open returns (nil, error) for invalid mode character ('x') in Lua 5.1
-- Reference: Lua 5.1 manual - io.open
-- @compat-notes: Lua 5.1 returns (nil, error); Lua 5.2+ throws error

local f, err = io.open("/tmp/test_file.txt", "x")
print("returns nil:", f == nil)
print("error string:", err)
print("error contains 'invalid':", (err or ""):find("nvalid") ~= nil or (err or ""):find("Invalid") ~= nil)
return f == nil and err ~= nil
