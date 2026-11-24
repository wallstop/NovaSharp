-- @lua-versions: 5.4, 5.5
-- @novasharp-only: false
-- @expects-error: false
-- @source: src/tests/WallstopStudios.NovaSharp.Interpreter.Tests.TUnit/Modules/IoLinesVersionParityTUnitTests.cs:205
-- @test: IoLinesVersionParityTUnitTests.IoLinesClosesItsFileOnScopeExitAndAtEndOfInput
-- Compatibility notes: Test targets Lua 5.4+
local path = os.tmpname()
                local input = assert(io.open(path, 'w'))
                assert(input:write('first\nsecond\n'))
                assert(input:close())

                local file
                local function check()
                    local iter, state, control
                    iter, state, control, file = io.lines(path)
                    for line in iter, state, control, file do
                        assert(line == 'first' and io.type(file) == 'file')
                        break
                    end
                    assert(io.type(file) == 'closed file', 'break did not close file')

                    iter, state, control, file = io.lines(path)
                    for line in iter, state, control, file do
                        assert(file:close())
                        break
                    end
                    assert(io.type(file) == 'closed file', 'manual close must survive scope exit')
                    local ok, problem = pcall(iter)
                    assert(not ok and string.find(problem, 'file is already closed', 1, true),
                        'manual close must invalidate iterator')

                    iter, state, control, file = io.lines(path)
                    assert(iter() == 'first')
                    assert(iter() == 'second')
                    assert(io.type(file) == 'file')
                    assert(iter() == nil)
                    assert(io.type(file) == 'closed file', 'iterator must close its file at EOF')
                end

                local ok, problem = pcall(check)
                if io.type(file) == 'file' then file:close() end
                assert(os.remove(path))
                assert(ok, problem)
