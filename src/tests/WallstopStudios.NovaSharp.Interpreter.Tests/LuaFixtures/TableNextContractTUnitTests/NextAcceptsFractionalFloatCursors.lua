-- @lua-versions: 5.1+
-- @novasharp-only: false
-- @expects-error: false
-- @source: src/tests/WallstopStudios.NovaSharp.Interpreter.Tests.TUnit/Units/DataTypes/TableNextContractTUnitTests.cs:417
-- @test: TableNextContractTUnitTests.NextAcceptsFractionalFloatCursors
-- Compatibility notes: Test targets Lua 5.3+
local function check()
                    local t = { [1.5] = true }
                    if not pcall(next, t, 1.5) then return 'fractional cursor rejected' end
                    return 'ok'
                end

                local outcome = check()
                print(outcome)
                return outcome
