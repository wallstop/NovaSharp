-- @lua-versions: 5.1+
-- @novasharp-only: false
-- @expects-error: true
-- @source: src\tests\WallstopStudios.NovaSharp.Interpreter.Tests.TUnit\Modules\StringModuleTUnitTests.cs:40
-- @test: StringModuleTUnitTests.CharReturnsNullByteForZero
return string.char(0)
