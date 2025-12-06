-- @lua-versions: 5.1+
-- @novasharp-only: false
-- @expects-error: false
-- @source: src/tests/NovaSharp.Interpreter.Tests.TUnit/Modules/StringModuleTUnitTests.cs:851
-- @test: StringModuleTUnitTests.FormatStringWithFieldWidth
return string.format('%10s', 'Hello')
