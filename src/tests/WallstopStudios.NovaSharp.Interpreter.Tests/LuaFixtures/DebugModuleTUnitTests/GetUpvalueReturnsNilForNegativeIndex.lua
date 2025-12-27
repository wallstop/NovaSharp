-- @lua-versions: 5.1
-- @novasharp-only: false
-- @expects-error: false
-- @source: src/tests/WallstopStudios.NovaSharp.Interpreter.Tests.TUnit/Modules/DebugModuleTUnitTests.cs:271
-- @test: DebugModuleTUnitTests.GetUpvalueReturnsNilForNegativeIndex
-- @compat-notes: Test targets Lua 5.1
local x = 10
                local function f() return x end
                return debug.getupvalue(f, -1)
