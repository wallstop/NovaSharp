-- @lua-versions: 5.1+
-- @novasharp-only: false
-- @expects-error: false
-- @source: src\tests\WallstopStudios.NovaSharp.Interpreter.Tests.TUnit\Modules\StringModuleTUnitTests.cs:934
-- @test: StringModuleTUnitTests.FormatOctalAlternateFlagWithZero
return string.format('%#o', 0)
