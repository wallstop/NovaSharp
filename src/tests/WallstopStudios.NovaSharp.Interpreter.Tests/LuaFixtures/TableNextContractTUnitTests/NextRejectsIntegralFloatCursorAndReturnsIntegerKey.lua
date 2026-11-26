-- @lua-versions: 5.4, 5.5
-- @novasharp-only: false
-- @expects-error: false
-- @source: src/tests/WallstopStudios.NovaSharp.Interpreter.Tests.TUnit/Units/DataTypes/TableNextContractTUnitTests.cs:355
-- @test: TableNextContractTUnitTests.NextRejectsIntegralFloatCursorAndReturnsIntegerKey
-- Compatibility notes: Test targets Lua 5.4+; Lua 5.3+: math.type (5.3+)
local function check()
                    local huge = 9007199254740992.0
                    local t = { [huge] = true }

                    local key = next(t)
                    if math.type(key) ~= 'integer' then
                        return 'stored key is ' .. math.type(key)
                    end

                    local ok, message = pcall(next, t, huge)
                    if ok then return 'integral float cursor accepted' end
                    if not tostring(message):find("invalid key to 'next'", 1, true) then
                        return tostring(message)
                    end

                    local negativeZero = { [-0.0] = true }
                    if math.type(next(negativeZero)) ~= 'integer' then
                        return 'negative zero key is ' .. math.type(next(negativeZero))
                    end
                    if pcall(next, negativeZero, -0.0) then
                        return 'negative zero cursor accepted'
                    end

                    return 'ok'
                end

                local outcome = check()
                print(outcome)
                return outcome
