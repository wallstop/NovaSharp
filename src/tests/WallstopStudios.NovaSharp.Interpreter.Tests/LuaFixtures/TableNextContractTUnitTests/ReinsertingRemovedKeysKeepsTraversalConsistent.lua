-- @lua-versions: 5.1+
-- @novasharp-only: false
-- @expects-error: false
-- @source: src/tests/WallstopStudios.NovaSharp.Interpreter.Tests.TUnit/Units/DataTypes/TableNextContractTUnitTests.cs:291
-- @test: TableNextContractTUnitTests.ReinsertingRemovedKeysKeepsTraversalConsistent
local function check()
                    -- Churn removes and re-adds keys many times; the traversal must always report
                    -- the live set exactly, no matter how the storage recycles or compacts entries.
                    local t = {}
                    for round = 1, 8 do
                        for i = 1, 50 do t['k' .. i] = round end
                        for i = 1, 50, 2 do t['k' .. i] = nil end

                        local live = 0
                        for k, v in pairs(t) do
                            if v ~= round then return 'stale value at ' .. k end
                            live = live + 1
                        end
                        if live ~= 25 then return 'round ' .. round .. ' live ' .. live end

                        for i = 1, 50, 2 do t['k' .. i] = round end
                    end

                    local final = 0
                    for _ in pairs(t) do final = final + 1 end
                    return tostring(final)
                end

                local outcome = check()
                print(outcome)
                return outcome
