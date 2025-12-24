-- @lua-versions: 5.1, 5.2, 5.3, 5.4, 5.5
-- @novasharp-only: true
-- @expects-error: false
-- @source: src/tests/WallstopStudios.NovaSharp.Interpreter.Tests.TUnit/Spec/LuaVersionGlobalTUnitTests.cs:131
-- @test: LuaVersionGlobalTUnitTests.NovaSharpTableContainsVersionInfo
-- @compat-notes: _NovaSharp is a NovaSharp-specific global; not comparable against native Lua
return _NovaSharp.version
