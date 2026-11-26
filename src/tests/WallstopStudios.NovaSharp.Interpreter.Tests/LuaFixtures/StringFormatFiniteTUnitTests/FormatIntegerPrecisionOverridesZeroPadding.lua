-- @lua-versions: 5.1+
-- @novasharp-only: false
-- @expects-error: false
-- @source: src/tests/WallstopStudios.NovaSharp.Interpreter.Tests.TUnit/Units/CoreLib/StringFormatFiniteTUnitTests.cs:104
-- @test: StringFormatFiniteTUnitTests.FormatIntegerPrecisionOverridesZeroPadding
return string.format('%05.3d', 2) .. ',' .. string.format('%05.3o', 2)
