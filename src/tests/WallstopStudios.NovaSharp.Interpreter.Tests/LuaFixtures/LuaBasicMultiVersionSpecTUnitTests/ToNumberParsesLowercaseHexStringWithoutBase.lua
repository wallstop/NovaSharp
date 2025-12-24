-- @lua-versions: 5.1, 5.2, 5.3, 5.4, 5.5
-- @novasharp-only: false
-- @expects-error: false
-- @source: src/tests/WallstopStudios.NovaSharp.Interpreter.Tests.TUnit/Spec/LuaBasicMultiVersionSpecTUnitTests.cs:139
-- @test: LuaBasicMultiVersionSpecTUnitTests.ToNumberParsesLowercaseHexStringWithoutBase
-- @compat-notes: Test targets Lua 5.1
return tonumber('0xff')
