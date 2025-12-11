-- @lua-versions: 5.1+
-- @novasharp-only: false
-- @expects-error: false
-- @source: src\tests\WallstopStudios.NovaSharp.Interpreter.Tests.TUnit\Modules\StringModuleTUnitTests.cs:1171
-- @test: StringModuleTUnitTests.FormatNegativeIntegerWithZeroPadding
return string.format('%08d', -42)
