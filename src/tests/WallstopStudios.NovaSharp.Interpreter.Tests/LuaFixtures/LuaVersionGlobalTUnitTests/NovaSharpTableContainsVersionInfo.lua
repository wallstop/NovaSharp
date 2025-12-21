-- @lua-versions: 5.1, 5.5
-- @novasharp-only: false
-- @expects-error: false
-- @source: src/tests/WallstopStudios.NovaSharp.Interpreter.Tests.TUnit/Spec/LuaVersionGlobalTUnitTests.cs:131
-- @test: LuaVersionGlobalTUnitTests.NovaSharpTableContainsVersionInfo
-- @compat-notes: Test targets Lua 5.1
return _NovaSharp.version
