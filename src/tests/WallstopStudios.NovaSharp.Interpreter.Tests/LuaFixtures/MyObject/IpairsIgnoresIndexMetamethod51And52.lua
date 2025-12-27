-- @lua-versions: 5.1
-- @novasharp-only: false
-- @expects-error: false
-- @source: src/tests/WallstopStudios.NovaSharp.Interpreter.Tests.TUnit/EndToEnd/MetatableTUnitTests.cs:350
-- @test: MyObject.IpairsIgnoresIndexMetamethod51And52
-- @compat-notes: Test targets Lua 5.1
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
