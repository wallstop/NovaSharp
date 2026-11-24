-- @lua-versions: novasharp-only
-- @novasharp-only: true
-- @expects-error: false
-- @source: src/tests/WallstopStudios.NovaSharp.Interpreter.Tests.TUnit/EndToEnd/TableIteratorMetamethodsTUnitTests.cs:180
-- @test: TableIteratorMetamethodsTUnitTests.NestedHostChunkKeepsIteratorHookNonYieldable
-- Compatibility notes: Test targets Lua 5.2+
local target = setmetatable({}, {__pairs = function()
                    assert(runChunk() == 1)
                    coroutine.yield('unexpected yield')
                end})
                local task = coroutine.create(function() return pairs(target) end)
                local ok, value = coroutine.resume(task)
                assert(not ok and string.find(value, 'yield', 1, true))
                assert(coroutine.status(task) == 'dead')
