-- @lua-versions: 5.1+
-- @novasharp-only: false
-- @expects-error: false
-- @source: src/tests/WallstopStudios.NovaSharp.Interpreter.Tests.TUnit/Modules/StringModuleTUnitTests.cs:481
-- @test: StringModuleTUnitTests.RepSupportsSeparatorsAndZeroCount
return string.rep('ab', 0)
