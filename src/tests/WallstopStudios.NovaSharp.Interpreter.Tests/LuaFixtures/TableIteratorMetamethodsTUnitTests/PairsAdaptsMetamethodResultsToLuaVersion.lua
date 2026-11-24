-- @lua-versions: 5.1+
-- @novasharp-only: false
-- @expects-error: false
-- @source: src/tests/WallstopStudios.NovaSharp.Interpreter.Tests.TUnit/EndToEnd/TableIteratorMetamethodsTUnitTests.cs:18
-- @test: TableIteratorMetamethodsTUnitTests.PairsAdaptsMetamethodResultsToLuaVersion
local supportsHook = _VERSION ~= 'Lua 5.1'
                local supportsClose = _VERSION == 'Lua 5.5'
                local expectedCount = supportsClose and 4 or 3
                local closes, hookCalls = 0, 0
                local closer = setmetatable({}, {__close = function() closes = closes + 1 end})
                local function done() end
                local meta = {__pairs = function(...)
                    assert(select('#', ...) == 1, 'hook should only receive the iterated table')
                    hookCalls = hookCalls + 1
                    return done, nil, nil, closer, 'discard'
                end}
                local target = setmetatable({}, meta)
                assert(select('#', pairs({})) == expectedCount, 'default result count')
                assert(select('#', pairs(target, 'discard')) == expectedCount, 'hook result count')
                for key in pairs(target) do error('unexpected key') end
                assert(hookCalls == (supportsHook and 2 or 0), 'hook availability')
                assert(closes == (supportsClose and 1 or 0), 'fourth-result lifetime')

                meta.__pairs = function() return done end
                local generator, state, control, close = pairs(target)
                if supportsHook then
                    assert(generator == done and state == nil and control == nil and close == nil)
                end
                assert(select('#', pairs(target)) == expectedCount, 'padded hook result count')

                meta.__pairs = done
                if supportsHook then
                    local first = pairs(target)
                    assert(first == nil and select('#', first) == 1, 'explicit nil from a zero-result hook')

                    meta.__pairs = function() return done, done(), 0 end
                    local _, middle, last = pairs(target)
                    assert(middle == nil and select('#', middle) == 1 and last == 0, 'explicit nil before control')
                end

                meta.__pairs = type
                if supportsHook then
                    assert(pairs(target) == 'table')
                end
                assert(select('#', pairs(target)) == expectedCount, 'padded built-in hook result count')

                local function checkProtected(ok, ...)
                    assert(ok and select('#', ...) == expectedCount, 'protected iterator result count')
                end
                for _, hook in ipairs({function() end, setmetatable({}, {__call = function() end}), type}) do
                    meta.__pairs = hook
                    checkProtected(pcall(pairs, target))
                    if _VERSION == 'Lua 5.4' or supportsClose then
                        checkProtected(xpcall(pairs, function(err) return err end, target))
                    end
                end
                if supportsHook then
                    meta.__pairs = false
                    local ok, problem = pcall(pairs, target)
                    assert(not ok and string.find(problem, 'attempt to call a boolean value', 1, true))
                end
