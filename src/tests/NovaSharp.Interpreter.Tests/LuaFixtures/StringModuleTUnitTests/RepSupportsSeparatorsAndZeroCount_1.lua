-- @lua-versions: 5.1+
-- @novasharp-only: false
-- @expects-error: false
-- @source: src/tests/NovaSharp.Interpreter.Tests.TUnit/Modules/StringModuleTUnitTests.cs:222
-- @test: StringModuleTUnitTests.RepSupportsSeparatorsAndZeroCount
return string.rep('ab', 0)
