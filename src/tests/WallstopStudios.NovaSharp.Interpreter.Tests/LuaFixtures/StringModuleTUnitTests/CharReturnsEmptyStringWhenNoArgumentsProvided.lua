-- @lua-versions: 5.1+
-- @novasharp-only: false
-- @expects-error: true
-- @source: src\tests\WallstopStudios.NovaSharp.Interpreter.Tests.TUnit\Modules\StringModuleTUnitTests.cs:60
-- @test: StringModuleTUnitTests.CharReturnsEmptyStringWhenNoArgumentsProvided
return string.char()
