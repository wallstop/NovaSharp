-- @lua-versions: 5.1, 5.5
-- @novasharp-only: false
-- @expects-error: false
-- @source: src/tests/WallstopStudios.NovaSharp.Interpreter.Tests.TUnit/Spec/LuaVersionGlobalTUnitTests.cs:149
-- @test: LuaVersionGlobalTUnitTests.NovaSharpTableContainsLuaCompatInfo
-- @compat-notes: Test targets Lua 5.1
return _NovaSharp.luacompat
