-- @lua-versions: 5.1+
-- @novasharp-only: false
-- @expects-error: false
-- @source: src/tests/WallstopStudios.NovaSharp.Interpreter.Tests.TUnit/Modules/StringModuleTUnitTests.cs:189
-- @test: StringModuleTUnitTests.ByteReturnsByteCodesForSubstring
local codes = {string.byte('Lua', 1, 3)}
                return #codes, codes[1], codes[2], codes[3]
