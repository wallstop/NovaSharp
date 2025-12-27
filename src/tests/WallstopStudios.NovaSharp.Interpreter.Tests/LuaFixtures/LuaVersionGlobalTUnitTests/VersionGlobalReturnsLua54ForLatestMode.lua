-- @lua-versions: 5.4, 5.5
-- @novasharp-only: false
-- @expects-error: false
-- @source: src/tests/WallstopStudios.NovaSharp.Interpreter.Tests.TUnit/Spec/LuaVersionGlobalTUnitTests.cs:49
-- @test: LuaVersionGlobalTUnitTests.VersionGlobalReturnsLua54ForLatestMode
-- @compat-notes: Test targets Lua 5.4+
return _VERSION
