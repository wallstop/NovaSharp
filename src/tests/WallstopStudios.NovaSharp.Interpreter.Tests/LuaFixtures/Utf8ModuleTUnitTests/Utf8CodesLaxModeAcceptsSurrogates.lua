-- @lua-versions: 5.4, 5.5
-- @novasharp-only: false
-- @expects-error: false
-- @source: src/tests/WallstopStudios.NovaSharp.Interpreter.Tests.TUnit/Modules/Utf8ModuleTUnitTests.cs
-- @test: Utf8ModuleTUnitTests.Utf8CodesLaxModeAcceptsSurrogates
-- @compat-notes: Lua 5.4+ utf8 lax mode allows lone surrogates

-- Test: utf8.codes with lax=true should accept lone surrogates
-- Reference: Lua 5.4 manual §6.5

-- Test a valid surrogate (U+D83D is a high surrogate)
local invalid = "\xED\xA0\xBD"  -- UTF-8 encoding of U+D83D (lone high surrogate)

-- With lax=true: should iterate without error and return the surrogate
local codepoints = {}
for pos, cp in utf8.codes(invalid, true) do
    table.insert(codepoints, cp)
end

-- The surrogate value is 0xD83D = 55357
return #codepoints == 1 and codepoints[1] == 0xD83D
