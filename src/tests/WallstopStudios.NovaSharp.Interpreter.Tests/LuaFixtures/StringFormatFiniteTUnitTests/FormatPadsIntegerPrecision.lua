-- @lua-versions: 5.1+
-- @novasharp-only: false
-- @expects-error: false
-- @source: src/tests/WallstopStudios.NovaSharp.Interpreter.Tests.TUnit/Units/CoreLib/StringFormatFiniteTUnitTests.cs:72
-- @test: StringFormatFiniteTUnitTests.FormatPadsIntegerPrecision
local outcome = table.concat({
                    string.format('%.5d', 42),
                    string.format('%.5o', 42),
                    string.format('%.5x', 42),
                    string.format('%.5u', 42)
                }, ',') print(outcome) return outcome
