-- @lua-versions: 5.2, 5.3, 5.4, 5.5
-- @novasharp-only: false
-- @expects-error: false
-- @source: src/tests/WallstopStudios.NovaSharp.Interpreter.Tests.TUnit/EndToEnd/TableIteratorMetamethodsTUnitTests.cs:85
-- @test: TableIteratorMetamethodsTUnitTests.PairsMetamethodCanYieldStartingInLua54
-- Compatibility notes: Test targets Lua 5.2+
local meta = {}
                local target = setmetatable({}, meta)
                local function hook()
                    coroutine.yield('paused')
                    return 1, 2, 3, 4, 5
                end
                local function check(fn, payload, invoke)
                    meta.__pairs = fn
                    local task = coroutine.create(function() return select('#', invoke(pairs, target)) end)
                    local ok, value = coroutine.resume(task)
                    if _VERSION == 'Lua 5.2' or _VERSION == 'Lua 5.3' then
                        assert(not ok and string.find(value, 'yield', 1, true))
                    else
                        assert(ok and value == payload)
                        ok, value = coroutine.resume(task, 1, 2, 3, 4, 5)
                        local count = _VERSION == 'Lua 5.5' and 4 or 3
                        assert(ok and value == (invoke == pcall and count + 1 or count))
                    end
                end
                local function direct(fn, target) return fn(target) end
                check(hook, 'paused', direct)
                check(setmetatable({}, {__call = hook}), 'paused', direct)
                check(coroutine.yield, target, direct)
                check(function()
                    local inner = setmetatable({}, {__pairs = function() return nil end})
                    pairs(inner)
                    return hook()
                end, 'paused', direct)
                if _VERSION == 'Lua 5.4' or _VERSION == 'Lua 5.5' then
                    check(coroutine.yield, target, pcall)
                end
