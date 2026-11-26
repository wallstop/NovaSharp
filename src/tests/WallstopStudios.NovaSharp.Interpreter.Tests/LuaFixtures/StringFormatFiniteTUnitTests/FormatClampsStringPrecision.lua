-- @lua-versions: 5.1+
-- @novasharp-only: false
-- @expects-error: false
-- @source: src/tests/WallstopStudios.NovaSharp.Interpreter.Tests.TUnit/Units/CoreLib/StringFormatFiniteTUnitTests.cs:60
-- @test: StringFormatFiniteTUnitTests.FormatClampsStringPrecision
local outcome = string.format('%.5s', 'abc') .. ',' .. string.format('%.2s', 'xyz') print(outcome) return outcome
