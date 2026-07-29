-- @lua-versions: 5.1+
-- @novasharp-only: false
-- @expects-error: false
-- @source: src/tests/WallstopStudios.NovaSharp.Interpreter.Tests.TUnit/Modules/StringModuleTUnitTests.cs:1473
-- @test: StringModuleTUnitTests.FormatStringWithLeftAlign
return string.format('%-10s', 'Hello')
