-- @lua-versions: 5.1+
-- @novasharp-only: false
-- @expects-error: false
-- @source: src/tests/NovaSharp.Interpreter.Tests.TUnit/Modules/StringModuleTUnitTests.cs:287
-- @test: StringModuleTUnitTests.SubHandlesNegativeIndices
return string.sub('NovaSharp', -5, -2)
