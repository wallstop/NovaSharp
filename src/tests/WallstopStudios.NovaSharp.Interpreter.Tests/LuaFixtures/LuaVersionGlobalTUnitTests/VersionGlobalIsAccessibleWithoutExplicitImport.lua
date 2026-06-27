-- @lua-versions: 5.1
-- @novasharp-only: false
-- @expects-error: false
-- @source: src\tests\WallstopStudios.NovaSharp.Interpreter.Tests.TUnit\Spec\LuaVersionGlobalTUnitTests.cs:66
-- @test: LuaVersionGlobalTUnitTests.VersionGlobalIsAccessibleWithoutExplicitImport
-- Test targets Lua 5.1
local v = _VERSION; return type(v)
