-- @lua-versions: 5.1+
-- @novasharp-only: false
-- @expects-error: false
-- @source: src/tests/WallstopStudios.NovaSharp.Interpreter.Tests.TUnit/Units/CoreLib/StringFormatFiniteTUnitTests.cs:155
-- @test: StringFormatFiniteTUnitTests.FormatClampsAlternateHexWidth
local outcome = string.format('%0#1x', 42)
                print(outcome)
                return outcome
