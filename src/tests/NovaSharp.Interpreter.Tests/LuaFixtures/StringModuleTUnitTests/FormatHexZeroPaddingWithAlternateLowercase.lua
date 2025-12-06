-- @lua-versions: 5.1+
-- @novasharp-only: false
-- @expects-error: false
-- @source: src/tests/NovaSharp.Interpreter.Tests.TUnit/Modules/StringModuleTUnitTests.cs:659
-- @test: StringModuleTUnitTests.FormatHexZeroPaddingWithAlternateLowercase
return string.format('%#08x', 255)
