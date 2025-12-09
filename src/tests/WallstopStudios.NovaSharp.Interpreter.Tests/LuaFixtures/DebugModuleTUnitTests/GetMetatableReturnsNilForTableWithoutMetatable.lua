-- @lua-versions: 5.3, 5.4, 5.5
-- @novasharp-only: false
-- @expects-error: false
-- @source: src/tests/WallstopStudios.NovaSharp.Interpreter.Tests.TUnit/Modules/DebugModuleTUnitTests.cs:1209
-- @test: DebugModuleTUnitTests.GetMetatableReturnsNilForTableWithoutMetatable
-- @compat-notes: Lua 5.3+: bitwise operators
local t = {}
                return debug.getmetatable(t)
