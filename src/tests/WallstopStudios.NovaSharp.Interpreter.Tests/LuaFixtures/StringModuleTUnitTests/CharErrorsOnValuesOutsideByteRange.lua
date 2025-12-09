-- @lua-versions: 5.1+
-- @novasharp-only: false
-- @expects-error: true
-- @source: src/tests/NovaSharp.Interpreter.Tests.TUnit/Modules/StringModuleTUnitTests.cs:68
-- @test: StringModuleTUnitTests.CharErrorsOnValuesOutsideByteRange
-- Standard Lua behavior: string.char throws for values outside 0-255
return string.char(-1, 256)
