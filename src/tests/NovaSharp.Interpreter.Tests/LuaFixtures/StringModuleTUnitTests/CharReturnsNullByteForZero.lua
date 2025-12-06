-- @lua-versions: 5.1+
-- @novasharp-only: false
-- @expects-error: true
-- @source: src/tests/NovaSharp.Interpreter.Tests.TUnit/Modules/StringModuleTUnitTests.cs:39
-- @test: StringModuleTUnitTests.CharReturnsNullByteForZero
return string.char(0)
