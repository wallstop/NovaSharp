-- @lua-versions: 5.4, 5.5
-- @novasharp-only: false
-- @expects-error: false
-- @source: src/tests/WallstopStudios.NovaSharp.Interpreter.Tests.TUnit/Modules/Utf8ModuleTUnitTests.cs
-- @test: Utf8ModuleTUnitTests.Utf8LenLaxModeAcceptsSurrogates
-- @compat-notes: Lua 5.4+ utf8 lax mode allows lone surrogates

-- Test: utf8.len with lax=true should accept lone surrogates
-- Reference: Lua 5.4 manual §6.5

-- Test a valid surrogate (U+D83D is a high surrogate)
-- In NovaSharp, strings are UTF-16 internally, so we use the C# char directly
-- In Lua 5.4, we'd need to construct the UTF-8 byte sequence

-- Without lax: should return nil + position
local invalid = "\xED\xA0\xBD"  -- UTF-8 encoding of U+D83D (lone high surrogate)
local strict_result, strict_pos = utf8.len(invalid)

-- With lax=true: should return the count
local lax_result = utf8.len(invalid, 1, -1, true)

-- Return results for verification
return strict_result == nil and type(strict_pos) == "number" and lax_result == 1
