-- @lua-versions: 5.1
-- @novasharp-only: false
-- @expects-error: false
-- @source: src/tests/WallstopStudios.NovaSharp.Interpreter.Tests.TUnit/Modules/DebugModuleTUnitTests.cs:1906
-- @test: DebugModuleTUnitTests.GetMetatableReturnsNilForTableWithoutMetatable
-- @compat-notes: Test targets Lua 5.1
local t = {}
                return debug.getmetatable(t)
