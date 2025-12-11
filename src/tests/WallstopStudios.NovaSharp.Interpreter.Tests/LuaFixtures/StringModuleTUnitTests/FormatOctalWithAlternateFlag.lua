-- @lua-versions: 5.1+
-- @novasharp-only: false
-- @expects-error: false
-- @source: src\tests\WallstopStudios.NovaSharp.Interpreter.Tests.TUnit\Modules\StringModuleTUnitTests.cs:925
-- @test: StringModuleTUnitTests.FormatOctalWithAlternateFlag
return string.format('%#o', 8)
