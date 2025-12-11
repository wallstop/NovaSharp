-- @lua-versions: 5.1+
-- @novasharp-only: false
-- @expects-error: false
-- @source: src\tests\WallstopStudios.NovaSharp.Interpreter.Tests.TUnit\Modules\StringModuleTUnitTests.cs:215
-- @test: StringModuleTUnitTests.ByteReturnsNilForEmptySource
return string.byte('', 1)
