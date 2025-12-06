-- @lua-versions: 5.1+
-- @novasharp-only: false
-- @expects-error: false
-- @source: src/tests/NovaSharp.Interpreter.Tests.TUnit/Modules/StringModuleTUnitTests.cs:829
-- @test: StringModuleTUnitTests.FormatCharFromNumber
return string.format('%c', 65)
