-- @lua-versions: 5.1+
-- @novasharp-only: false
-- @expects-error: false
-- @source: src/tests/NovaSharp.Interpreter.Tests.TUnit/Modules/StringModuleTUnitTests.cs:650
-- @test: StringModuleTUnitTests.FormatHexWithPrecision
return string.format('%.4x', 255)
