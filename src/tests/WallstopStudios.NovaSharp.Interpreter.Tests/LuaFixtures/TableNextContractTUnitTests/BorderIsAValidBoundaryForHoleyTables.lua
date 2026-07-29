-- @lua-versions: 5.1+
-- @novasharp-only: false
-- @expects-error: false
-- @source: src/tests/WallstopStudios.NovaSharp.Interpreter.Tests.TUnit/Units/DataTypes/TableNextContractTUnitTests.cs:195
-- @test: TableNextContractTUnitTests.BorderIsAValidBoundaryForHoleyTables
local function check()
                    -- A border n satisfies t[n] ~= nil and t[n+1] == nil, for any table shape.
                    local function isBorder(t)
                        local n = #t
                        if n == 0 then return t[1] == nil end
                        return t[n] ~= nil and t[n + 1] == nil
                    end

                    local cases = {
                        {},
                        { 1 },
                        { 1, 2, 3 },
                        { 1, 2, nil, 4 },
                        { nil, 2 },
                        { 1, nil, nil, nil, 5 },
                    }

                    for i = 1, 6 do
                        if not isBorder(cases[i]) then return 'constructor case ' .. i end
                    end

                    local grown = {}
                    for i = 1, 33 do
                        grown[i] = i
                        if #grown ~= i then return 'append border at ' .. i end
                    end

                    -- Shrinking only guarantees *a* border, not a particular one, so assert the
                    -- invariant rather than an exact length.
                    for i = 33, 1, -1 do
                        grown[i] = nil
                        if not isBorder(grown) then return 'shrink border at ' .. i end
                    end

                    return 'ok'
                end

                local outcome = check()
                print(outcome)
                return outcome
