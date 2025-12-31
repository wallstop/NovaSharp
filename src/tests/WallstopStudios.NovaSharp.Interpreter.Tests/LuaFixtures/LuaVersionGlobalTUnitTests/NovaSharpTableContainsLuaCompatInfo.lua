-- @lua-versions: novasharp-only
-- @novasharp-only: true
-- @expects-error: false
-- @source: src\tests\WallstopStudios.NovaSharp.Interpreter.Tests.TUnit\Spec\LuaVersionGlobalTUnitTests.cs:149
-- @test: LuaVersionGlobalTUnitTests.NovaSharpTableContainsLuaCompatInfo
-- @compat-notes: NovaSharp: NovaSharp global; Test targets Lua 5.1
return _NovaSharp.luacompat
