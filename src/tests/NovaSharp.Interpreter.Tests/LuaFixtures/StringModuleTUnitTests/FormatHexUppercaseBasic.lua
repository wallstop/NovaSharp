-- @lua-versions: 5.1+
-- @novasharp-only: false
-- @expects-error: false
-- @source: src/tests/NovaSharp.Interpreter.Tests.TUnit/Modules/StringModuleTUnitTests.cs:596
-- @test: StringModuleTUnitTests.FormatHexUppercaseBasic
return string.format('%X', 255)
