-- @lua-versions: 5.1+
-- @novasharp-only: false
-- @expects-error: false
-- @source: src/tests/WallstopStudios.NovaSharp.Interpreter.Tests.TUnit/Units/CoreLib/StringFormatFiniteTUnitTests.cs:118
-- @test: StringFormatFiniteTUnitTests.FormatSuppressesZeroAtZeroIntegerPrecision
-- Compatibility notes: Test targets Lua 5.4+
return table.concat({
                    '[' .. string.format('%.d', 0) .. ']',
                    '[' .. string.format('%.o', 0) .. ']',
                    '[' .. string.format('%.x', 0) .. ']',
                    '[' .. string.format('%.u', 0) .. ']',
                    string.format('%.d', 42)
                }, ',')
