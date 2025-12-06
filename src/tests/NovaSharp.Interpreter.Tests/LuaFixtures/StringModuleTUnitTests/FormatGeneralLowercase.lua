-- @lua-versions: 5.1+
-- @novasharp-only: false
-- @expects-error: false
-- @source: src/tests/NovaSharp.Interpreter.Tests.TUnit/Modules/StringModuleTUnitTests.cs:798
-- @test: StringModuleTUnitTests.FormatGeneralLowercase
return string.format('%g', 0.0001234)
