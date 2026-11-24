-- @lua-versions: 5.4, 5.5
-- @novasharp-only: false
-- @expects-error: false
-- @source: src/tests/WallstopStudios.NovaSharp.Interpreter.Tests.TUnit/EndToEnd/ValueSlotSemanticsTUnitTests.cs:225
-- @test: ValueSlotSemanticsTUnitTests.GenericForDispatchesCloseMetamethodAtExit
-- Compatibility notes: Test targets Lua 5.4+
local resource
                local seenCoroutine
                local called = false
                local target = setmetatable({}, {__call = function(_, value, err)
                    assert(value == resource and err == nil)
                    seenCoroutine = coroutine.running()
                    called = true
                end})
                local meta = {__close = target}
                resource = setmetatable({}, meta)
                local function once(_, previous)
                    if previous == 0 then return 1 end
                end
                for _ in once, nil, 0, resource do assert(not called) end
                assert(called, 'callable close')

                local function checkInvalid(replacement, errorType)
                    meta.__close = target
                    local ok, message = pcall(function()
                        for _ in once, nil, 0, resource do meta.__close = replacement end
                    end)
                    local expected = 'attempt to call a ' .. errorType .. " value (metamethod 'close')"
                    assert(not ok and string.find(message, expected, 1, true), tostring(message))
                end
                checkInvalid(false, 'boolean')
                checkInvalid(nil, 'nil')
                checkInvalid(setmetatable({}, {__call = false}), 'boolean')

                called = false
                meta.__close = target
                local task = coroutine.create(function()
                    for _ in once, nil, 0, resource do coroutine.yield() end
                end)
                assert(coroutine.resume(task) and not called)
                assert(coroutine.close(task) and called, 'close suspended loop')

                local completed = coroutine.create(function()
                    for _ in once, nil, 0, resource do end
                end)
                assert(coroutine.resume(completed) and seenCoroutine == completed, 'active coroutine')
