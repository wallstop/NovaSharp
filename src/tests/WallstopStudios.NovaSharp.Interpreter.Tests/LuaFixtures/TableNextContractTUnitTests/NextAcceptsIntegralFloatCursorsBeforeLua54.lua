-- @lua-versions: 5.1, 5.2, 5.3
-- @novasharp-only: false
-- @expects-error: false
-- @source: src/tests/WallstopStudios.NovaSharp.Interpreter.Tests.TUnit/Units/DataTypes/TableNextContractTUnitTests.cs:441
-- @test: TableNextContractTUnitTests.NextAcceptsIntegralFloatCursorsBeforeLua54
-- Compatibility notes: Test targets Lua 5.3+
local function check()
                    local huge = 9007199254740992.0
                    local t = { [huge] = true }
                    if not pcall(next, t, huge) then return 'rejected' end
                    return 'ok'
                end

                local outcome = check()
                print(outcome)
                return outcome
