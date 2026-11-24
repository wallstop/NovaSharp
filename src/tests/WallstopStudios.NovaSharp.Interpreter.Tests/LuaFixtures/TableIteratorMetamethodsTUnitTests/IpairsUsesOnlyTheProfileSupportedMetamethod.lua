-- @lua-versions: 5.1+
-- @novasharp-only: false
-- @expects-error: false
-- @source: src/tests/WallstopStudios.NovaSharp.Interpreter.Tests.TUnit/EndToEnd/TableIteratorMetamethodsTUnitTests.cs:122
-- @test: TableIteratorMetamethodsTUnitTests.IpairsUsesOnlyTheProfileSupportedMetamethod
local called = false
                local function done() end
                local meta = {__ipairs = function()
                    called = true
                    return done, nil, nil, true
                end}
                local target = setmetatable({ 'default' }, meta)
                local seen = 0
                for index, value in ipairs(target) do
                    assert(index == 1 and value == 'default')
                    seen = seen + 1
                end
                -- Stock Lua 5.3 enables LUA_COMPAT_IPAIRS.
                assert(called == (_VERSION == 'Lua 5.2' or _VERSION == 'Lua 5.3'), '__ipairs availability')
                assert(seen == (called and 0 or 1))
                assert(select('#', ipairs(target)) == 3, 'ipairs result count')

                meta.__ipairs = function() return done end
                assert(select('#', ipairs(target)) == 3, 'padded ipairs result count')
