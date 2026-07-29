-- @lua-versions: novasharp-only
-- @novasharp-only: true
-- @expects-error: false
-- @source: src/tests/WallstopStudios.NovaSharp.Interpreter.Tests.TUnit/Units/DataTypes/TableNextContractTUnitTests.cs:248
-- @test: TableNextContractTUnitTests.SparseAndDenseIntegerKeysCoexist
-- Compatibility notes: Uses injected variable: array
local function check()
                    -- Forces a dense prefix into the array part and keys far past it into the hash
                    -- part, then checks every key still reads back and is traversed exactly once.
                    local t = {}
                    for i = 1, 64 do t[i] = i end
                    for _, k in ipairs({ 1000, 5000, 100000 }) do t[k] = k end

                    for i = 1, 64 do
                        if t[i] ~= i then return 'dense miss ' .. i end
                    end
                    for _, k in ipairs({ 1000, 5000, 100000 }) do
                        if t[k] ~= k then return 'sparse miss ' .. k end
                    end

                    local total = 0
                    for k, v in pairs(t) do
                        if k ~= v then return 'key/value drift ' .. tostring(k) end
                        total = total + 1
                    end

                    if #t ~= 64 then return 'border ' .. #t end
                    return tostring(total)
                end

                local outcome = check()
                print(outcome)
                return outcome
