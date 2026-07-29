-- @lua-versions: 5.1+
-- @novasharp-only: false
-- @expects-error: false
-- @source: src/tests/WallstopStudios.NovaSharp.Interpreter.Tests.TUnit/Units/DataTypes/TableNextContractTUnitTests.cs:33
-- @test: TableNextContractTUnitTests.PairsVisitsEveryKeyExactlyOnce
local function check()
                    local t = {}
                    for i = 1, 40 do t[i] = i end
                    t.alpha = 'a'
                    t.beta = 'b'
                    t[500] = 'sparse'
                    t[-3] = 'negative'
                    t[0] = 'zero'
                    t[2.5] = 'fractional'
                    t[true] = 'boolean'

                    local seen = {}
                    local count = 0
                    for k in pairs(t) do
                        if seen[k] then return 'duplicate: ' .. tostring(k) end
                        seen[k] = true
                        count = count + 1
                    end

                    local expected = 40 + 2 + 1 + 1 + 1 + 1 + 1
                    if count ~= expected then
                        return 'count ' .. count .. ' expected ' .. expected
                    end

                    for i = 1, 40 do if not seen[i] then return 'missing ' .. i end end
                    for _, k in ipairs({ 'alpha', 'beta', 500, -3, 0, 2.5, true }) do
                        if not seen[k] then return 'missing ' .. tostring(k) end
                    end

                    return 'ok'
                end

                local outcome = check()
                print(outcome)
                return outcome
