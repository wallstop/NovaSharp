-- @lua-versions: novasharp-only
-- @novasharp-only: true
-- @expects-error: false
-- @source: src/tests/WallstopStudios.NovaSharp.Interpreter.Tests.TUnit/EndToEnd/TableIteratorMetamethodsTUnitTests.cs:161
-- @test: TableIteratorMetamethodsTUnitTests.UserDataIpairsCannotPassAnIteratorClosingValue
local function check(fn, expected)
                    local task = coroutine.create(fn)
                    local ok, marker = coroutine.resume(task)
                    assert(ok and marker == 'paused')
                    ok, marker = coroutine.resume(task, function() end, nil, nil, true)
                    assert(ok and marker == expected)
                end
                check(function() return select('#', ipairs(iteratorUserData)) end, 3)
                check(function()
                    for index in ipairs(iteratorUserData) do error('unexpected index') end
                    return 'done'
                end, 'done')
