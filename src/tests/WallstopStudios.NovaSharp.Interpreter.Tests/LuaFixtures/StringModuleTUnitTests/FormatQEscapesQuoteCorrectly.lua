-- @lua-versions: 5.1+
-- @novasharp-only: false
-- @expects-error: false
-- @source: src\tests\WallstopStudios.NovaSharp.Interpreter.Tests.TUnit\Modules\StringModuleTUnitTests.cs:2662
-- @test: StringModuleTUnitTests.FormatQEscapesQuoteCorrectly
return string.format('%q', '"')
