-- @lua-versions: 5.1+
-- @novasharp-only: false
-- @expects-error: false
-- @source: src/tests/WallstopStudios.NovaSharp.Interpreter.Tests.TUnit/Units/CoreLib/StringFormatFiniteTUnitTests.cs:33
-- @test: StringFormatFiniteTUnitTests.FormatTreatsEmptyPrecisionAsZero
local outcome = string.format('%.g', 1.5) .. ',' .. string.format('%.e', 1.5) print(outcome) return outcome
