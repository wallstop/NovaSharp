-- @lua-versions: 5.1+
-- @novasharp-only: false
-- @expects-error: false
-- @source: src/tests/WallstopStudios.NovaSharp.Interpreter.Tests.TUnit/EndToEnd/ValueSlotSemanticsTUnitTests.cs:82
-- @test: ValueSlotSemanticsTUnitTests.GenericForLocalsStartAsNil
local state = { 10, 20 }
                local function step(s, previous)
                    assert(s == state, 'iterator state')
                    local index = previous + 1
                    if s[index] then return index, s[index] end
                end

                local readers = {}
                for index, value in step, state, 0 do
                    local first, second, third, fourth
                    assert(first == nil, 'first local captured iterator state')
                    assert(second == nil and third == nil and fourth == nil, 'remaining locals')
                    for _, nestedValue in ipairs({ value }) do
                        local nested
                        assert(nested == nil and nestedValue == value, 'nested iterator')
                    end
                    readers[index] = function() return first, second end
                    if index == 1 then first, second = value, index end
                end

                local first, second = readers[1]()
                assert(first == 10 and second == 1, 'first iteration capture')
                first, second = readers[2]()
                assert(first == nil and second == nil, 'second iteration capture')

                for index = 1, 1 do
                    local fresh
                    assert(fresh == nil, 'numeric loop control')
                end

                local function empty() end
                for index in step, state, 0 do
                    local explicit = nil
                    local fromEmpty = empty()
                    assert(explicit == nil and fromEmpty == nil, 'explicit initializers')
                    break
                end
