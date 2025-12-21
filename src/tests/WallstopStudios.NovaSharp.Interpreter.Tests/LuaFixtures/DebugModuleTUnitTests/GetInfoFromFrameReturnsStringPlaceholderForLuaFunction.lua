-- @lua-versions: 5.1, 5.5
-- @novasharp-only: false
-- @expects-error: false
-- @source: src/tests/WallstopStudios.NovaSharp.Interpreter.Tests.TUnit/Modules/DebugModuleTUnitTests.cs:895
-- @test: DebugModuleTUnitTests.GetInfoFromFrameReturnsStringPlaceholderForLuaFunction
-- @compat-notes: Test targets Lua 5.1
local function probe()
                    local info = debug.getinfo(1, 'f')
                    return info.func
                end
                return probe()
