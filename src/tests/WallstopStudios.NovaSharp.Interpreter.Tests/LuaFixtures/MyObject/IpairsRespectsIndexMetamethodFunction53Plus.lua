-- @lua-versions: 5.3, 5.4, 5.5
-- @novasharp-only: false
-- @expects-error: false
-- @source: src/tests/WallstopStudios.NovaSharp.Interpreter.Tests.TUnit/EndToEnd/MetatableTUnitTests.cs:292
-- @test: MyObject.IpairsRespectsIndexMetamethodFunction53Plus
-- @compat-notes: Test targets Lua 5.3+
local underlying = {10, 20, 30}
                local proxy = {}
                setmetatable(proxy, {
                    __index = function(t, k)
                        return underlying[k]
                    end
                })
                local result = ''
                for i, v in ipairs(proxy) do
                    result = result .. i .. ':' .. v .. ' '
                end
                return result
