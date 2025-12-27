-- @lua-versions: 5.1
-- @novasharp-only: false
-- @expects-error: false
-- @source: src/tests/WallstopStudios.NovaSharp.Interpreter.Tests.TUnit/Spec/LuaBasicMultiVersionSpecTUnitTests.cs:139
-- @test: LuaBasicMultiVersionSpecTUnitTests.ToNumberParsesLowercaseHexStringWithoutBase
-- @compat-notes: Test targets Lua 5.1
return tonumber('0xff')
