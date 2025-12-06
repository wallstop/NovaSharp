-- @lua-versions: 5.1+
-- @novasharp-only: false
-- @expects-error: false
-- @source: src/tests/NovaSharp.Interpreter.Tests.TUnit/Modules/StringModuleTUnitTests.cs:296
-- @test: StringModuleTUnitTests.FormatInterpolatesValues
return string.format('Value: %0.2f', 3.14159)
