-- @lua-versions: 5.1+
-- @novasharp-only: false
-- @expects-error: false
-- @source: src/tests/NovaSharp.Interpreter.Tests.TUnit/Modules/StringModuleTUnitTests.cs:767
-- @test: StringModuleTUnitTests.FormatFloatWithPositiveSpace
return string.format('% f', 3.14)
