-- @lua-versions: 5.1, 5.2, 5.3, 5.4, 5.5
-- @novasharp-only: false
-- @expects-error: false
-- @source: src/tests/WallstopStudios.NovaSharp.Interpreter.Tests.TUnit/Spec/LuaVersionGlobalTUnitTests.cs:108
-- @test: LuaVersionGlobalTUnitTests.VersionGlobalCanBeUsedForVersionDetection
-- @compat-notes: Test targets Lua 5.1
local major, minor = _VERSION:match('Lua (%d+)%.(%d+)')
                return tonumber(major), tonumber(minor)
