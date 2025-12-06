-- @lua-versions: 5.1+
-- @novasharp-only: false
-- @expects-error: false
-- @source: src/tests/NovaSharp.Interpreter.Tests.TUnit/Modules/StringModuleTUnitTests.cs:587
-- @test: StringModuleTUnitTests.FormatHexLowercaseBasic
return string.format('%x', 255)
