-- @lua-versions: novasharp-only
-- @novasharp-only: true
-- @expects-error: false
-- @source: src/tests/WallstopStudios.NovaSharp.Interpreter.Tests.TUnit/Modules/StringModuleTUnitTests.cs:697
-- @test: StringModuleTUnitTests.UnicodeReturnsFullUnicodeCodePoints
-- @compat-notes: NovaSharp: NovaSharp string extension; Test targets Lua 5.1
local codes = {string.unicode('\u{0100}')}
                return #codes, codes[1]
