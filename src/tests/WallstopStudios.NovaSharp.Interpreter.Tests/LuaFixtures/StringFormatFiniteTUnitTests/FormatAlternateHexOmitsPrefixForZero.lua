-- @lua-versions: 5.1+
-- @novasharp-only: false
-- @expects-error: false
-- @source: src/tests/WallstopStudios.NovaSharp.Interpreter.Tests.TUnit/Units/CoreLib/StringFormatFiniteTUnitTests.cs:138
-- @test: StringFormatFiniteTUnitTests.FormatAlternateHexOmitsPrefixForZero
-- Compatibility notes: Test targets Lua 5.4+
local outcome = table.concat({
                    string.format('%#x', 0),
                    '[' .. string.format('%#.x', 0) .. ']',
                    '[' .. string.format('%#5.x', 0) .. ']',
                    string.format('%#x', 42)
                }, ',') print(outcome) return outcome
