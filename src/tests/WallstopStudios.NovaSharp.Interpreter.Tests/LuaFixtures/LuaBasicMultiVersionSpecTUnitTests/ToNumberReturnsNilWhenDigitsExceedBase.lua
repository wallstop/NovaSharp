-- @lua-versions: 5.1, 5.5
-- @novasharp-only: false
-- @expects-error: false
-- @source: src/tests/WallstopStudios.NovaSharp.Interpreter.Tests.TUnit/Spec/LuaBasicMultiVersionSpecTUnitTests.cs:56
-- @test: LuaBasicMultiVersionSpecTUnitTests.ToNumberReturnsNilWhenDigitsExceedBase
-- @compat-notes: Test targets Lua 5.1
return tonumber('2', 2), tonumber('g', 16)
