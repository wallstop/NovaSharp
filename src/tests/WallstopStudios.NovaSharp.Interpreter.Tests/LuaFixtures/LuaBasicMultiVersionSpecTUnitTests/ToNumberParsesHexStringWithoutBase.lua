-- @lua-versions: 5.1
-- @novasharp-only: false
-- @expects-error: false
-- @source: src/tests/WallstopStudios.NovaSharp.Interpreter.Tests.TUnit/Spec/LuaBasicMultiVersionSpecTUnitTests.cs:120
-- @test: LuaBasicMultiVersionSpecTUnitTests.ToNumberParsesHexStringWithoutBase
-- @compat-notes: Test targets Lua 5.1
return tonumber('0xFF')
