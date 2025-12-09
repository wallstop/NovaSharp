-- @lua-versions: 5.1+
-- @novasharp-only: false
-- @expects-error: false
-- @source: src\tests\WallstopStudios.NovaSharp.Interpreter.Tests.TUnit\Modules\StringModuleTUnitTests.cs:1144
-- @test: StringModuleTUnitTests.FormatIntegerPositiveSignOverridesSpace
return string.format('%+ d', 42)
