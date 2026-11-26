-- @lua-versions: 5.1+
-- @novasharp-only: false
-- @expects-error: false
-- @source: src/tests/WallstopStudios.NovaSharp.Interpreter.Tests.TUnit/Units/CoreLib/StringFormatFiniteTUnitTests.cs:90
-- @test: StringFormatFiniteTUnitTests.FormatPreservesSpaceSignWithZeroPadding
return string.format('%0 9g', 1.5) .. ',' .. string.format('%0 9f', 1.5)
