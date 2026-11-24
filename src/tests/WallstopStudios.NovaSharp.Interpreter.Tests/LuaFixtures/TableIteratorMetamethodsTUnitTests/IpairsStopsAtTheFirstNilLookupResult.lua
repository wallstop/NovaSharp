-- @lua-versions: 5.1+
-- @novasharp-only: false
-- @expects-error: false
-- @source: src/tests/WallstopStudios.NovaSharp.Interpreter.Tests.TUnit/EndToEnd/TableIteratorMetamethodsTUnitTests.cs:229
-- @test: TableIteratorMetamethodsTUnitTests.IpairsStopsAtTheFirstNilLookupResult
local usesIndex = _VERSION ~= 'Lua 5.1' and _VERSION ~= 'Lua 5.2'
                local terminalCount = _VERSION == 'Lua 5.1' and 0 or 1
                for _, returnsExtra in ipairs({false, true}) do
                    local calls = 0
                    local target = setmetatable({}, {__index = function(_, index)
                        calls = calls + 1
                        if index == 1 then return false, 'discard' end
                        if index == 2 then return 'value', 'discard' end
                        if returnsExtra then return nil, 'discard' end
                    end})
                    local iterator, state, start = ipairs(target)
                    local function checkTermination(index)
                        local function check(...)
                            assert(select('#', ...) == terminalCount and (...) == nil)
                        end
                        check(iterator(state, index))
                    end
                    if usesIndex then
                        local index, value, extra = iterator(state, start)
                        assert(index == 1 and value == false and extra == nil)
                        index, value, extra = iterator(state, index)
                        assert(index == 2 and value == 'value' and extra == nil)
                        checkTermination(index)
                        assert(calls == 3)
                    else
                        checkTermination(start)
                        assert(calls == 0)
                    end

                    local count = 0
                    for _, value in ipairs(target) do
                        count = count + 1
                        assert(count <= 2, 'nil lookup leaked a loop iteration')
                        assert((count == 1 and value == false) or (count == 2 and value == 'value'))
                    end
                    assert(count == (usesIndex and 2 or 0))
                end
