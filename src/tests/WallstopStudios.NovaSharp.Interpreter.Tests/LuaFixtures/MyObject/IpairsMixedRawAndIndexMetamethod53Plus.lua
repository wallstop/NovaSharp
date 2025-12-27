-- @lua-versions: 5.3, 5.4, 5.5
-- @novasharp-only: false
-- @expects-error: false
-- @source: src/tests/WallstopStudios.NovaSharp.Interpreter.Tests.TUnit/EndToEnd/MetatableTUnitTests.cs:377
-- @test: MyObject.IpairsMixedRawAndIndexMetamethod53Plus
-- @compat-notes: Test targets Lua 5.3+
local underlying = {'a', 'b', 'c', 'd', 'e'}
                local proxy = {nil, 'B', nil}
                setmetatable(proxy, {
                    __index = underlying
                })
                local result = ''
                for i, v in ipairs(proxy) do
                    result = result .. i .. ':' .. v .. ' '
                end
                return result
