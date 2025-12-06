-- @lua-versions: 5.1+
-- @novasharp-only: false
-- @expects-error: false
-- @source: src/tests/NovaSharp.Interpreter.Tests.TUnit/Modules/StringModuleTUnitTests.cs:467
-- @test: StringModuleTUnitTests.CharAcceptsNumericStringArguments
return string.char('65', '66')
