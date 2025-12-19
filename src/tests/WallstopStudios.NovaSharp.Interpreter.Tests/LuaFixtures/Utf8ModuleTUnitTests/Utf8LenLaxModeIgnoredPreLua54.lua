-- @lua-versions: 5.3
-- @novasharp-only: false
-- @expects-error: false
-- @source: src/tests/WallstopStudios.NovaSharp.Interpreter.Tests.TUnit/Modules/Utf8ModuleTUnitTests.cs
-- @test: Utf8ModuleTUnitTests.Utf8LenLaxModeIgnoredPreLua54
-- @compat-notes: Lua 5.3 ignores the 4th argument (lax not supported)

-- Test: In Lua 5.3, the lax parameter should be ignored
-- Reference: Lua 5.3 manual §6.5 (no lax parameter)

-- Test a valid surrogate (U+D83D is a high surrogate)
local invalid = "\xED\xA0\xBD"  -- UTF-8 encoding of U+D83D (lone high surrogate)

-- Even with true as 4th arg, should return nil + position in Lua 5.3
local result, pos = utf8.len(invalid, 1, -1, true)

-- Should still fail (lax ignored)
return result == nil and type(pos) == "number"
