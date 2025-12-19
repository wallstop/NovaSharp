-- @lua-versions: 5.4, 5.5
-- @novasharp-only: false
-- @expects-error: false
-- @source: src/tests/WallstopStudios.NovaSharp.Interpreter.Tests.TUnit/Modules/Utf8ModuleTUnitTests.cs
-- @test: Utf8ModuleTUnitTests.Utf8CodepointLaxModeAcceptsSurrogates
-- @compat-notes: Lua 5.4+ utf8 lax mode allows lone surrogates

-- Test: utf8.codepoint with lax=true should accept lone surrogates
-- Reference: Lua 5.4 manual §6.5

-- Test a valid surrogate (U+D83D is a high surrogate)
local invalid = "\xED\xA0\xBD"  -- UTF-8 encoding of U+D83D (lone high surrogate)

-- With lax=true: should return the surrogate code point value
local lax_result = utf8.codepoint(invalid, 1, 1, true)

-- The surrogate value is 0xD83D = 55357
return lax_result == 0xD83D
