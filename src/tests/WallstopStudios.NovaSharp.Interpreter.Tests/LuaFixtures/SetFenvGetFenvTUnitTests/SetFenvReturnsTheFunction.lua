-- @lua-versions: 5.1
-- @novasharp-only: false
-- @expects-error: false
-- @source: src/tests/WallstopStudios.NovaSharp.Interpreter.Tests.TUnit/Modules/SetFenvGetFenvTUnitTests.cs:154
-- @test: SetFenvGetFenvTUnitTests.SetFenvReturnsTheFunction
-- @compat-notes: Test targets Lua 5.1
local function f() return 1 end
                local returned = setfenv(f, _G)
                return returned == f
