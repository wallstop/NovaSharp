-- @lua-versions: 5.1+
-- @novasharp-only: false
-- @expects-error: false
-- @source: src\tests\WallstopStudios.NovaSharp.Interpreter.Tests.TUnit\Modules\StringModuleTUnitTests.cs:518
-- @test: StringModuleTUnitTests.ReverseReturnsEmptyStringForEmptyInput
return string.reverse('')
