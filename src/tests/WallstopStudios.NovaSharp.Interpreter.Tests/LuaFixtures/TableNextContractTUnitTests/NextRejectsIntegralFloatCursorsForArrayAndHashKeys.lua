-- @lua-versions: 5.4, 5.5
-- @novasharp-only: false
-- @expects-error: false
-- @source: src/tests/WallstopStudios.NovaSharp.Interpreter.Tests.TUnit/Units/DataTypes/TableNextContractTUnitTests.cs:391
-- @test: TableNextContractTUnitTests.NextRejectsIntegralFloatCursorsForArrayAndHashKeys
-- Compatibility notes: Test targets Lua 5.4+
local function check()
                    local array = { 10, 20, 30 }
                    if pcall(next, array, 2.0) then return 'array cursor accepted' end

                    local hash = { [500] = true }
                    if pcall(next, hash, 500.0) then return 'hash cursor accepted' end

                    return 'ok'
                end

                local outcome = check()
                print(outcome)
                return outcome
