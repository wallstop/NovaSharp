-- @lua-versions: 5.1+
-- @novasharp-only: false
-- @expects-error: false
-- @source: src/tests/NovaSharp.Interpreter.Tests.TUnit/Modules/StringModuleTUnitTests.cs:187
-- @test: StringModuleTUnitTests.ByteAcceptsIntegralFloatIndices
return string.byte('Lua', 1.0)
