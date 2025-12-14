-- @lua-versions: 5.3, 5.4, 5.5
-- @novasharp-only: false
-- @expects-error: true
-- @error-pattern: number has no integer representation
-- @source: src/tests/NovaSharp.Interpreter.Tests.TUnit/Modules/StringModuleTUnitTests.cs
-- @test: StringModuleTUnitTests.ByteErrorsOnNonIntegerIndexLua53Plus
-- Lua 5.3+ require exact integer representation for string.byte indices.
-- Non-integer floats like 1.5 throw "number has no integer representation".
return string.byte('Lua', 1.5)
