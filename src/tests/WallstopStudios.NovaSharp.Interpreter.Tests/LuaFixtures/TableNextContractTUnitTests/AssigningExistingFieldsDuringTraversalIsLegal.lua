-- @lua-versions: 5.1+
-- @novasharp-only: false
-- @expects-error: false
-- @source: src/tests/WallstopStudios.NovaSharp.Interpreter.Tests.TUnit/Units/DataTypes/TableNextContractTUnitTests.cs:83
-- @test: TableNextContractTUnitTests.AssigningExistingFieldsDuringTraversalIsLegal
local function check()
                    local t = {}
                    for i = 1, 20 do t[i] = i end
                    t.name = 'value'
                    t.other = 'value'

                    local visited = 0
                    for k in pairs(t) do
                        t[k] = 'rewritten'
                        visited = visited + 1
                    end

                    local cleared = 0
                    for k in pairs(t) do
                        t[k] = nil
                        cleared = cleared + 1
                    end

                    local remaining = 0
                    for _ in pairs(t) do remaining = remaining + 1 end

                    return visited .. ',' .. cleared .. ',' .. remaining
                end

                local outcome = check()
                print(outcome)
                return outcome
