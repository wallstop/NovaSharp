-- @lua-versions: 5.1+
-- @novasharp-only: false
-- @expects-error: true
-- @source: src/tests/WallstopStudios.NovaSharp.Interpreter.Tests.TUnit/Modules/StringModuleTUnitTests.cs:81
-- @test: StringModuleTUnitTests.CharErrorsOnValuesOutsideByteRange
return string.char(-1, 256)
