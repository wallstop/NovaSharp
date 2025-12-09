-- @lua-versions: 5.3, 5.4
-- @novasharp-only: false
-- @expects-error: true
-- @error-pattern: number has no integer representation
-- @source: src/tests/NovaSharp.Interpreter.Tests.TUnit/Modules/StringModuleTUnitTests.cs
-- @test: StringModuleTUnitTests.ByteErrorsOnNaNIndexLua53Plus
-- Lua 5.3+ require exact integer representation for string.byte indices.
-- NaN has no integer representation.
return string.byte('Lua', 0/0)
