-- @lua-versions: 5.1+
-- @novasharp-only: false
-- @expects-error: false
-- @source: src/tests/WallstopStudios.NovaSharp.Interpreter.Tests.TUnit/Modules/IoLinesVersionParityTUnitTests.cs:30
-- @test: IoLinesVersionParityTUnitTests.IoLinesReturnsOwnedIteratorAndRejectsCallsAfterEof
-- Compatibility notes: Uses injected variable: file
local path = os.tmpname()
                local output = assert(io.open(path, 'w'))
                assert(output:write('first\n'))
                assert(output:close())

                local function check(...)
                    local iterator, state, control, owner = ...
                    local line = iterator()
                    local eofCount = select('#', iterator())
                    for i = 1, 2 do
                        local ok, problem = pcall(iterator)
                        assert(not ok and string.find(problem, 'file is already closed', 1, true),
                            'closed iterator must report a Lua error')
                    end

                    local hasCloseValue = _VERSION == 'Lua 5.4' or _VERSION == 'Lua 5.5'
                    assert(select('#', ...) == (hasCloseValue and 4 or 1), 'io.lines result count')
                    assert(type(iterator) == 'function' and state == nil and control == nil,
                        'io.lines iterator shape')
                    assert(io.type(owner) == (hasCloseValue and 'closed file' or nil), 'owned file')
                    assert(line == 'first' and eofCount == 0, 'line and EOF results')
                end

                local ok, problem = pcall(function() check(io.lines(path)) end)
                assert(os.remove(path))
                assert(ok, problem)
