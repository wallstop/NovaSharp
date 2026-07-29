-- @lua-versions: 5.1+
-- @novasharp-only: false
-- @expects-error: false
-- @source: src/tests/WallstopStudios.NovaSharp.Interpreter.Tests.TUnit/Units/DataTypes/TableNextContractTUnitTests.cs:158
-- @test: TableNextContractTUnitTests.NextStillAdvancesAfterTheCurrentKeyIsCleared
local function check()
                    -- Clearing the key you are standing on is the one mutation the manual permits
                    -- mid-traversal, so next() must still resolve that key as a cursor afterwards.
                    local t = { 10, 20, 30, 40 }
                    t.tail = 'end'

                    local visited = 0
                    local key = next(t)
                    while key ~= nil do
                        visited = visited + 1
                        local current = key
                        key = next(t, current)
                        t[current] = nil
                    end

                    local remaining = 0
                    for _ in pairs(t) do remaining = remaining + 1 end

                    return visited .. ',' .. remaining
                end

                local outcome = check()
                print(outcome)
                return outcome
