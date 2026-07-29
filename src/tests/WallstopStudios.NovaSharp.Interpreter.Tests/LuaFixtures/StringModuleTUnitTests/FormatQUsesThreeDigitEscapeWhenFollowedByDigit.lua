-- @lua-versions: 5.1+
-- @novasharp-only: false
-- @expects-error: false
-- @source: src\tests\WallstopStudios.NovaSharp.Interpreter.Tests.TUnit\Modules\StringModuleTUnitTests.cs:2737
-- @test: StringModuleTUnitTests.FormatQUsesThreeDigitEscapeWhenFollowedByDigit
return string.format('%q', string.char(0) .. '2')
