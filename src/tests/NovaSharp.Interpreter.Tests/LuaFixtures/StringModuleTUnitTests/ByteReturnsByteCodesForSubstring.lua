-- @lua-versions: 5.3, 5.4
-- @novasharp-only: false
-- @expects-error: false
-- @source: src/tests/NovaSharp.Interpreter.Tests.TUnit/Modules/StringModuleTUnitTests.cs:124
-- @test: StringModuleTUnitTests.ByteReturnsByteCodesForSubstring
-- @compat-notes: Lua 5.3+: bitwise operators
local codes = {string.byte('Lua', 1, 3)}
                return #codes, codes[1], codes[2], codes[3]
