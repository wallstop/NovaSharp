-- @lua-versions: 5.1+
-- @novasharp-only: false
-- @expects-error: false
-- @source: src/tests/NovaSharp.Interpreter.Tests.TUnit/Modules/StringModuleTUnitTests.cs:873
-- @test: StringModuleTUnitTests.FormatPercentEscape
return string.format('100%% complete')
