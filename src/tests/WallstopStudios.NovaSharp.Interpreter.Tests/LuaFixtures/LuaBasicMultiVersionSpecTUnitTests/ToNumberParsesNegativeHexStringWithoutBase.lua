-- @lua-versions: 5.1, 5.5
-- @novasharp-only: false
-- @expects-error: false
-- @source: src/tests/WallstopStudios.NovaSharp.Interpreter.Tests.TUnit/Spec/LuaBasicMultiVersionSpecTUnitTests.cs:177
-- @test: LuaBasicMultiVersionSpecTUnitTests.ToNumberParsesNegativeHexStringWithoutBase
-- @compat-notes: Test targets Lua 5.1
return tonumber('-0x10')
