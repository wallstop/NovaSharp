-- @lua-versions: 5.1+
-- @novasharp-only: false
-- @expects-error: false
-- @source: src/tests/NovaSharp.Interpreter.Tests.TUnit/Modules/StringModuleTUnitTests.cs:59
-- @test: StringModuleTUnitTests.CharReturnsEmptyStringWhenNoArgumentsProvided
return string.char()
