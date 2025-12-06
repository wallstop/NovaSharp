-- @lua-versions: 5.3, 5.4
-- @novasharp-only: false
-- @expects-error: false
-- @source: src/tests/NovaSharp.Interpreter.Tests.TUnit/Modules/StringModuleTUnitTests.cs:205
-- @test: StringModuleTUnitTests.UnicodeReturnsFullUnicodeCodePoints
-- @compat-notes: Lua 5.3+: bitwise operators
local codes = {string.unicode('\u{0100}')}
                return #codes, codes[1]
