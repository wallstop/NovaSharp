-- @lua-versions: 5.1, 5.5
-- @novasharp-only: false
-- @expects-error: false
-- @source: src/tests/WallstopStudios.NovaSharp.Interpreter.Tests.TUnit/Modules/DebugModuleTUnitTests.cs:1148
-- @test: DebugModuleTUnitTests.GetInfoWithEmptyWhatReturnsEmptyTable
-- @compat-notes: Test targets Lua 5.1
local function sample() end
                local info = debug.getinfo(sample, '')
                local count = 0
                for k, v in pairs(info) do count = count + 1 end
                return count
