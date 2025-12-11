-- @lua-versions: 5.1+
-- @novasharp-only: false
-- @expects-error: false
-- @source: src\tests\WallstopStudios.NovaSharp.Interpreter.Tests.TUnit\Modules\StringModuleTUnitTests.cs:1278
-- @test: StringModuleTUnitTests.FormatStringWithPrecision
return string.format('%.3s', 'Hello')
