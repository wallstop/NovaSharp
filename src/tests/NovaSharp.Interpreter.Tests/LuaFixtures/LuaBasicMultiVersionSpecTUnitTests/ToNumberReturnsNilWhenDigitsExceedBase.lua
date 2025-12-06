-- @lua-versions: 5.1+
-- @novasharp-only: false
-- @expects-error: false
-- @source: src/tests/NovaSharp.Interpreter.Tests.TUnit/Spec/LuaBasicMultiVersionSpecTUnitTests.cs:44
-- @test: LuaBasicMultiVersionSpecTUnitTests.ToNumberReturnsNilWhenDigitsExceedBase
return tonumber('2', 2), tonumber('g', 16)
