-- @lua-versions: 5.1+
-- @novasharp-only: false
-- @expects-error: false
-- @source: src/tests/NovaSharp.Interpreter.Tests.TUnit/Modules/StringModuleTUnitTests.cs:699
-- @test: StringModuleTUnitTests.FormatIntegerWithPositiveSpace
return string.format('% d', 42)
