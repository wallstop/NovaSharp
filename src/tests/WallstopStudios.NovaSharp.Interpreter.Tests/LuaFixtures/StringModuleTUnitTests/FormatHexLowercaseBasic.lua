-- @lua-versions: 5.1+
-- @novasharp-only: false
-- @expects-error: false
-- @source: src\tests\WallstopStudios.NovaSharp.Interpreter.Tests.TUnit\Modules\StringModuleTUnitTests.cs:1023
-- @test: StringModuleTUnitTests.FormatHexLowercaseBasic
return string.format('%x', 255)
