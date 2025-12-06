-- @lua-versions: 5.1+
-- @novasharp-only: false
-- @expects-error: false
-- @source: src/tests/NovaSharp.Interpreter.Tests.TUnit/Modules/StringModuleTUnitTests.cs:565
-- @test: StringModuleTUnitTests.FormatUnsignedWithFieldWidth
return string.format('%8u', 42)
