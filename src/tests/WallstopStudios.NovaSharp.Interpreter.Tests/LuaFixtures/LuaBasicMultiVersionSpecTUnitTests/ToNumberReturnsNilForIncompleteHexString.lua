-- @lua-versions: 5.1
-- @novasharp-only: false
-- @expects-error: false
-- @source: src/tests/WallstopStudios.NovaSharp.Interpreter.Tests.TUnit/Spec/LuaBasicMultiVersionSpecTUnitTests.cs:231
-- @test: LuaBasicMultiVersionSpecTUnitTests.ToNumberReturnsNilForIncompleteHexString
-- @compat-notes: Test targets Lua 5.1
return tonumber('0x')
