-- @lua-versions: 5.3, 5.4
-- @novasharp-only: false
-- @expects-error: false
-- @source: src/tests/NovaSharp.Interpreter.Tests.TUnit/Modules/DebugModuleTUnitTests.cs:572
-- @test: DebugModuleTUnitTests.GetInfoFromFrameReturnsStringPlaceholderForLuaFunction
-- @compat-notes: Lua 5.3+: bitwise operators
local function probe()
                    local info = debug.getinfo(1, 'f')
                    return info.func
                end
                return probe()
