-- @lua-versions: 5.1+
-- @novasharp-only: false
-- @expects-error: false
-- @source: src/tests/WallstopStudios.NovaSharp.Interpreter.Tests.TUnit/Units/CoreLib/StringFormatFiniteTUnitTests.cs:21
-- @test: StringFormatFiniteTUnitTests.FormatAcceptsZeroPaddingBeforeSignFlag
local outcome = string.format('%0+9g', 1.5) print(outcome) return outcome
