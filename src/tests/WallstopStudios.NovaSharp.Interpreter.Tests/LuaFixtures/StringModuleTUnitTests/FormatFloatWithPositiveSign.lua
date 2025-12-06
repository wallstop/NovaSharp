-- @lua-versions: 5.1+
-- @novasharp-only: false
-- @expects-error: false
-- @source: src/tests/NovaSharp.Interpreter.Tests.TUnit/Modules/StringModuleTUnitTests.cs:758
-- @test: StringModuleTUnitTests.FormatFloatWithPositiveSign
return string.format('%+f', 3.14)
