-- @lua-versions: 5.1+
-- @novasharp-only: false
-- @expects-error: false
-- @source: src/tests/WallstopStudios.NovaSharp.Interpreter.Tests.TUnit/Modules/TableModuleTUnitTests.cs:1148
-- @test: TableModuleTUnitTests.InsertPositionalUsesProfileLengthSemantics
-- Compatibility notes: Lua 5.3+: math.maxinteger (5.3+)
local lua51 = _VERSION == 'Lua 5.1'
                local lua52 = _VERSION == 'Lua 5.2'

                if not lua51 then
                    local wide = setmetatable({}, {__len = function() return 4294967298 end})
                    local endIndex = lua52 and 3 or 4294967299
                    assert(select('#', table.insert(wide, endIndex, 'new')) == 0, 'wide position')
                    assert(rawget(wide, endIndex) == 'new')

                    local shorter = setmetatable({3, 5, 7}, {__len = function() return 2.75 end})
                    local ok, errorText = pcall(table.insert, shorter, 2, 'new')
                    if lua52 then
                        assert(ok and shorter[2] == 'new' and shorter[3] == 5)
                    else
                        assert(not ok and shorter[2] == 5 and shorter[3] == 7)
                        assert(string.find(errorText, 'object length is not an integer', 1, true))
                    end

                    local calls = 0
                    local sparse = {[1] = 'head', [50] = 'middle', [101] = 'removed', [102] = 'kept'}
                    setmetatable(sparse, {__len = function(first, second)
                        assert(first == sparse and second == sparse, 'length operands')
                        calls = calls + 1
                        return 100
                    end})
                    table.insert(sparse, 2, 'new')
                    assert(calls == 1 and sparse[1] == 'head' and sparse[2] == 'new')
                    assert(sparse[50] == nil and sparse[51] == 'middle')
                    assert(sparse[101] == nil and sparse[102] == 'kept')
                end

                if not lua51 and not lua52 then
                    local high = 4294967298
                    local t = setmetatable({[high] = 'high', [high - 1] = 'previous'}, {
                        __len = function() return high end
                    })
                    table.insert(t, high - 1, 'new')
                    assert(t[high - 1] == 'new' and t[high] == 'previous' and t[high + 1] == 'high')

                    local unsignedBounds = _VERSION ~= 'Lua 5.3'
                    local wrapped = setmetatable({}, {__len = function() return math.maxinteger end})
                    local ok = pcall(table.insert, wrapped, 1, 'new')
                    assert(ok == unsignedBounds)
                    if unsignedBounds then
                        assert(wrapped[1] == 'new')
                    else
                        assert(next(wrapped) == nil)
                    end

                    local negative = setmetatable({[-2] = 'prior'}, {__len = function() return -2 end})
                    ok = pcall(table.insert, negative, -2, 'new')
                    assert(ok == unsignedBounds)
                    if unsignedBounds then
                        assert(negative[-2] == 'new' and negative[-1] == 'prior')
                    else
                        assert(negative[-2] == 'prior' and negative[-1] == nil)
                    end
                    assert(not pcall(table.insert, negative, 0, 'invalid'))
                end
