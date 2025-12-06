-- @lua-versions: 5.1+
-- @novasharp-only: false
-- @expects-error: false
-- @source: src/tests/NovaSharp.Interpreter.Tests.TUnit/Modules/StringModuleTUnitTests.cs:106
-- @test: StringModuleTUnitTests.LowerReturnsLowercaseString
return string.lower('NovaSharp')
