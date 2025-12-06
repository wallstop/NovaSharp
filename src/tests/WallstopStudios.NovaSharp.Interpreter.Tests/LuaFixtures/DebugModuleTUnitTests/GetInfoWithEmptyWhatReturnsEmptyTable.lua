-- @lua-versions: 5.3, 5.4
-- @novasharp-only: false
-- @expects-error: false
-- @source: src/tests/NovaSharp.Interpreter.Tests.TUnit/Modules/DebugModuleTUnitTests.cs:772
-- @test: DebugModuleTUnitTests.GetInfoWithEmptyWhatReturnsEmptyTable
-- @compat-notes: Lua 5.3+: bitwise operators
local function sample() end
                local info = debug.getinfo(sample, '')
                local count = 0
                for k, v in pairs(info) do count = count + 1 end
                return count
