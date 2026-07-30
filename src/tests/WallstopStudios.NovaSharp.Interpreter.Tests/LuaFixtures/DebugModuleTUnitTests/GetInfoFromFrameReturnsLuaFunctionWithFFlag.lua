-- @lua-versions: 5.1
-- @novasharp-only: false
-- @expects-error: false
-- @source: src/tests/WallstopStudios.NovaSharp.Interpreter.Tests.TUnit/Modules/DebugModuleTUnitTests.cs:1116
-- @test: DebugModuleTUnitTests.GetInfoFromFrameReturnsLuaFunctionWithFFlag
-- Compatibility notes: Test targets Lua 5.1
local function probe()
                    local info = debug.getinfo(1, 'f')
                    local funcInfo = debug.getinfo(info.func, 'S')
                    return type(info.func) .. ':' .. funcInfo.what .. ':' .. funcInfo.short_src
                end
                return probe()
