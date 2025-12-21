-- @lua-versions: 5.1, 5.5
-- @novasharp-only: false
-- @expects-error: false
-- @source: src/tests/WallstopStudios.NovaSharp.Interpreter.Tests.TUnit/Spec/LuaVersionGlobalTUnitTests.cs:37
-- @test: LuaVersionGlobalTUnitTests.VersionGlobalReturnsCorrectVersionPerMode
-- @compat-notes: Test targets Lua 5.1
return _VERSION
