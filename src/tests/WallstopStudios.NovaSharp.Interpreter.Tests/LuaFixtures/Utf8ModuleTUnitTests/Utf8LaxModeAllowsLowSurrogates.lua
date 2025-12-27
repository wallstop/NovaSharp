-- @lua-versions: 5.4, 5.5
-- @novasharp-only: false
-- @expects-error: false
-- @source: src/tests/WallstopStudios.NovaSharp.Interpreter.Tests.TUnit/Modules/Utf8ModuleTUnitTests.cs
-- @test: Utf8ModuleTUnitTests.Utf8LaxModeAllowsLowSurrogates
-- @compat-notes: Lua 5.4+ utf8 lax mode allows low surrogates (not just high)

-- Test: utf8 lax mode should accept low surrogates
-- Reference: Lua 5.4 manual §6.5

-- Test a low surrogate (U+DC00 is the first low surrogate)
local low_surr = "\xED\xB0\x80"  -- UTF-8 encoding of U+DC00 (lone low surrogate)

-- With lax=true: should work
local len_result = utf8.len(low_surr, 1, -1, true)
local cp_result = utf8.codepoint(low_surr, 1, 1, true)

-- U+DC00 = 56320
return len_result == 1 and cp_result == 0xDC00
