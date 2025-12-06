-- @lua-versions: 5.1+
-- @novasharp-only: false
-- @expects-error: false
-- @source: src/tests/NovaSharp.Interpreter.Tests.TUnit/Modules/StringModuleTUnitTests.cs:842
-- @test: StringModuleTUnitTests.FormatStringWithPrecision
return string.format('%.3s', 'Hello')
