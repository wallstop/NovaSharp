-- @lua-versions: 5.1+
-- @novasharp-only: false
-- @expects-error: false
-- @source: src/tests/WallstopStudios.NovaSharp.Interpreter.Tests.TUnit/Units/DataTypes/TableNextContractTUnitTests.cs:330
-- @test: TableNextContractTUnitTests.NextRejectsKeysThatAreNotInTheTable
local function check()
                    local t = { a = 1 }
                    local ok = pcall(next, t, 'missing')
                    return tostring(ok)
                end

                local outcome = check()
                print(outcome)
                return outcome
