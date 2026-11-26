-- @lua-versions: 5.1+
-- @novasharp-only: false
-- @expects-error: false
-- @source: src/tests/WallstopStudios.NovaSharp.Interpreter.Tests.TUnit/Units/CoreLib/StringFormatFiniteTUnitTests.cs:92
-- @test: StringFormatFiniteTUnitTests.FormatPreservesSpaceSignWithZeroPadding
local outcome = string.format('%0 9g', 1.5) .. ',' .. string.format('%0 9f', 1.5) print(outcome) return outcome
