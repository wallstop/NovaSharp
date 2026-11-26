-- @lua-versions: 5.1+
-- @novasharp-only: false
-- @expects-error: false
-- @source: src/tests/WallstopStudios.NovaSharp.Interpreter.Tests.TUnit/Units/CoreLib/StringFormatFiniteTUnitTests.cs:120
-- @test: StringFormatFiniteTUnitTests.FormatSuppressesZeroAtZeroIntegerPrecision
local outcome = table.concat({
                    '[' .. string.format('%.d', 0) .. ']',
                    '[' .. string.format('%.o', 0) .. ']',
                    '[' .. string.format('%.x', 0) .. ']',
                    '[' .. string.format('%.u', 0) .. ']',
                    string.format('%.d', 42)
                }, ',') print(outcome) return outcome
