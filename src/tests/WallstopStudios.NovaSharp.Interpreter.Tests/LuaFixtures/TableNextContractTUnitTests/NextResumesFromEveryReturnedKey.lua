-- @lua-versions: 5.1+
-- @novasharp-only: false
-- @expects-error: false
-- @source: src/tests/WallstopStudios.NovaSharp.Interpreter.Tests.TUnit/Units/DataTypes/TableNextContractTUnitTests.cs:123
-- @test: TableNextContractTUnitTests.NextResumesFromEveryReturnedKey
local function check()
                    local t = { 10, 20, 30 }
                    t.tail = 'end'

                    local count = 0
                    local key, value = next(t)
                    while key ~= nil do
                        count = count + 1
                        if value == nil then return 'nil value at ' .. tostring(key) end
                        key, value = next(t, key)
                    end

                    if next({}) ~= nil then return 'empty table must yield nil' end

                    return tostring(count)
                end

                local outcome = check()
                print(outcome)
                return outcome
