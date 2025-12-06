-- @lua-versions: 5.1+
-- @novasharp-only: false
-- @expects-error: false
-- @source: src/tests/NovaSharp.Interpreter.Tests.TUnit/Modules/StringModuleTUnitTests.cs:97
-- @test: StringModuleTUnitTests.LenReturnsStringLength
return string.len('Nova')
