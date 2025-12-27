-- @lua-versions: 5.1
-- @novasharp-only: false
-- @expects-error: false
-- @source: src/tests/WallstopStudios.NovaSharp.Interpreter.Tests.TUnit/EndToEnd/MetatableTUnitTests.cs:320
-- @test: MyObject.IpairsRespectsIndexMetamethodTable53Plus
-- @compat-notes: Test targets Lua 5.1
local underlying = {100, 200, 300, 400}
                local proxy = {}
                setmetatable(proxy, {
                    __index = underlying
                })
                local result = ''
                for i, v in ipairs(proxy) do
                    result = result .. i .. ':' .. v .. ' '
                end
                return result
