-- @lua-versions: 5.1+
-- @novasharp-only: false
-- @expects-error: false
-- @source: src/tests/WallstopStudios.NovaSharp.Interpreter.Tests.TUnit/Modules/StringModuleTUnitTests.cs:1400
-- @test: StringModuleTUnitTests.FormatDecimalWithFloatFallsBackToConversion
return string.format('%d', 123.456)
