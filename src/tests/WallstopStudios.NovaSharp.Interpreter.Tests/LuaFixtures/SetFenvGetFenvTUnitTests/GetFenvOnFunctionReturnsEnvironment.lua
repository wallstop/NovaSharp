-- @lua-versions: 5.1
-- @novasharp-only: false
-- @expects-error: false
-- @source: src/tests/WallstopStudios.NovaSharp.Interpreter.Tests.TUnit/Modules/SetFenvGetFenvTUnitTests.cs:119
-- @test: SetFenvGetFenvTUnitTests.GetFenvOnFunctionReturnsEnvironment
-- @compat-notes: Test targets Lua 5.1
local function f() return 1 end
                return getfenv(f) == _G
