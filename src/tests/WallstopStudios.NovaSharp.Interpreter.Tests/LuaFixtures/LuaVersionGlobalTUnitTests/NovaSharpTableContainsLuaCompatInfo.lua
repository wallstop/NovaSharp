-- @lua-versions: 5.1, 5.5
-- @novasharp-only: true
-- @expects-error: false
-- @source: src/tests/WallstopStudios.NovaSharp.Interpreter.Tests.TUnit/Spec/LuaVersionGlobalTUnitTests.cs:149
-- @test: LuaVersionGlobalTUnitTests.NovaSharpTableContainsLuaCompatInfo
-- @compat-notes: _NovaSharp is a NovaSharp-specific global; not comparable against native Lua
return _NovaSharp.luacompat
