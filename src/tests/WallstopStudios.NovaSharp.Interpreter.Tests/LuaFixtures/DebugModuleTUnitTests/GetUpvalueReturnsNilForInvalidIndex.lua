-- @lua-versions: 5.1
-- @novasharp-only: false
-- @expects-error: false
-- @source: src/tests/WallstopStudios.NovaSharp.Interpreter.Tests.TUnit/Modules/DebugModuleTUnitTests.cs:251
-- @test: DebugModuleTUnitTests.GetUpvalueReturnsNilForInvalidIndex
-- @compat-notes: Test targets Lua 5.1
local function f() end
                return debug.getupvalue(f, 999)
