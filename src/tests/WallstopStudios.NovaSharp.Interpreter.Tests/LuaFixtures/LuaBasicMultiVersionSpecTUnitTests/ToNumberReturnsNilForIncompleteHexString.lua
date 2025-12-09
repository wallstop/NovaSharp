-- @lua-versions: 5.1+
-- @novasharp-only: false
-- @expects-error: false
-- @source: src/tests/WallstopStudios.NovaSharp.Interpreter.Tests.TUnit/Spec/LuaBasicMultiVersionSpecTUnitTests.cs
-- @test: LuaBasicMultiVersionSpecTUnitTests.ToNumberReturnsNilForIncompleteHexString
-- "0x" without digits should return nil
return tonumber('0x')
