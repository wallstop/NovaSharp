-- @lua-versions: 5.1+
-- @novasharp-only: false
-- @expects-error: false
-- @source: src/tests/WallstopStudios.NovaSharp.Interpreter.Tests.TUnit/Units/CoreLib/StringFormatFiniteTUnitTests.cs:45
-- @test: StringFormatFiniteTUnitTests.FormatUsesTwoDigitMinimumExponent
local outcome = string.format('%e', 1.5) .. ',' .. string.format('%E', 0.015625) print(outcome) return outcome
