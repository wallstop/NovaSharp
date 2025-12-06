-- @lua-versions: 5.1+
-- @novasharp-only: false
-- @expects-error: false
-- @source: src/tests/NovaSharp.Interpreter.Tests.TUnit/Modules/StringModuleTUnitTests.cs:88
-- @test: StringModuleTUnitTests.CharTruncatesFloatValues
return string.char(65.5)
