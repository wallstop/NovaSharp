-- @lua-versions: 5.3, 5.4, 5.5
-- @novasharp-only: false
-- @expects-error: true
-- @source: src/tests/WallstopStudios.NovaSharp.Interpreter.Tests.TUnit/Modules/DebugModuleTUnitTests.cs:205
-- @test: DebugModuleTUnitTests.GetUpvalueReturnsNilForNegativeIndex
-- @compat-notes: Lua 5.3+: bitwise operators
local x = 10
                local function f() return x end
                return debug.getupvalue(f, -1)
