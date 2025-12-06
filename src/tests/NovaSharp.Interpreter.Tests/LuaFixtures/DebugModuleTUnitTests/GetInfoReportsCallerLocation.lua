-- @lua-versions: 5.3, 5.4
-- @novasharp-only: false
-- @expects-error: false
-- @source: src/tests/NovaSharp.Interpreter.Tests.TUnit/Modules/DebugModuleTUnitTests.cs:36
-- @test: DebugModuleTUnitTests.GetInfoReportsCallerLocation
-- @compat-notes: Lua 5.3+: bitwise operators
local function probe()
                    local info = debug.getinfo(1)
                    return info.short_src, info.currentline, info.what
                end
                return probe()
