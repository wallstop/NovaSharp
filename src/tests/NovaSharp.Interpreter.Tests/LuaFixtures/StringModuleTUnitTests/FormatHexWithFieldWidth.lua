-- @lua-versions: 5.1+
-- @novasharp-only: false
-- @expects-error: false
-- @source: src/tests/NovaSharp.Interpreter.Tests.TUnit/Modules/StringModuleTUnitTests.cs:623
-- @test: StringModuleTUnitTests.FormatHexWithFieldWidth
return string.format('%8x', 255)
