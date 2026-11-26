-- @lua-versions: 5.1, 5.2, 5.3
-- @novasharp-only: false
-- @expects-error: false
-- @source: src/tests/WallstopStudios.NovaSharp.Interpreter.Tests.TUnit/Units/DataTypes/TableNextContractTUnitTests.cs:449
-- @test: TableNextContractTUnitTests.NextAcceptsIntegralFloatCursorsBeforeLua54
-- Compatibility notes: Test targets Lua 5.3+
local function check()
                    local huge = 9007199254740992.0
                    local t = { [huge] = true }
                    if not pcall(next, t, huge) then return 'hash cursor rejected' end

                    local array = { 10, 20, 30 }
                    local arrayOk = pcall(next, array, 2.0)
                    if _VERSION == 'Lua 5.3' then
                        if arrayOk then return 'Lua 5.3 accepted array float cursor' end
                    elseif not arrayOk then
                        return 'pre-5.3 rejected array float cursor'
                    end

                    return 'ok'
                end

                local outcome = check()
                print(outcome)
                return outcome
