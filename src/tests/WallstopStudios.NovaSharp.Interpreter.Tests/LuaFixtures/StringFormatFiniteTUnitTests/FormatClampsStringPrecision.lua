-- @lua-versions: 5.1+
-- @novasharp-only: false
-- @expects-error: false
-- @source: src/tests/WallstopStudios.NovaSharp.Interpreter.Tests.TUnit/Units/CoreLib/StringFormatFiniteTUnitTests.cs:58
-- @test: StringFormatFiniteTUnitTests.FormatClampsStringPrecision
return string.format('%.5s', 'abc') .. ',' .. string.format('%.2s', 'xyz')
