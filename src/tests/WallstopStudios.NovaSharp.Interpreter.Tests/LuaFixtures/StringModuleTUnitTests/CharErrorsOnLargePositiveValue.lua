-- @lua-versions: 5.1+
-- @novasharp-only: false
-- @expects-error: true
-- @source: src/tests/NovaSharp.Interpreter.Tests.TUnit/Modules/StringModuleTUnitTests.cs
-- @test: StringModuleTUnitTests.CharErrorsOnOutOfRangeValue(300)
-- Standard Lua behavior: string.char throws for values well above 255
return string.char(300)
