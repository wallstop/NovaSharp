-- @lua-versions: 5.1+
-- @novasharp-only: false
-- @expects-error: false
-- @source: src/tests/NovaSharp.Interpreter.Tests.TUnit/Modules/StringModuleTUnitTests.cs:115
-- @test: StringModuleTUnitTests.UpperReturnsUppercaseString
return string.upper('NovaSharp')
