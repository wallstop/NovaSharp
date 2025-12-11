-- @lua-versions: 5.1+
-- @novasharp-only: false
-- @expects-error: false
-- @source: src\tests\WallstopStudios.NovaSharp.Interpreter.Tests.TUnit\Modules\StringModuleTUnitTests.cs:1225
-- @test: StringModuleTUnitTests.FormatExponentUppercase
return string.format('%E', 12345.6)
