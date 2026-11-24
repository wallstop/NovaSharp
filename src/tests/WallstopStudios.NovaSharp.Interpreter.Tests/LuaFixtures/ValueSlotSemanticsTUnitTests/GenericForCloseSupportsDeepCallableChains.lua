-- @lua-versions: 5.4, 5.5
-- @novasharp-only: false
-- @expects-error: false
-- @source: src/tests/WallstopStudios.NovaSharp.Interpreter.Tests.TUnit/EndToEnd/ValueSlotSemanticsTUnitTests.cs:321
-- @test: ValueSlotSemanticsTUnitTests.GenericForCloseSupportsDeepCallableChains
-- Compatibility notes: Test targets Lua 5.4+
local resource
                local isLua55 = _VERSION == 'Lua 5.5'
                local depth = isLua55 and 15 or 32
                local caller = debug.getinfo(1, 'fS')
                local called = false
                local closer = function(...)
                    local frame = debug.getinfo(2, 'fS')
                    assert(frame.func == caller.func and frame.what == caller.what)
                    local count = select('#', ...)
                    assert(count == depth + (isLua55 and 1 or 2))
                    assert(select(depth + 1, ...) == resource)
                    if not isLua55 then assert(select(depth + 2, ...) == nil) end
                    called = true
                end
                for _ = 1, depth do closer = setmetatable({}, {__call = closer}) end
                resource = setmetatable({}, {__close = closer})
                for _ in function() end, nil, nil, resource do end
                assert(called)

                if isLua55 then
                    resource = setmetatable({}, {__close = setmetatable({}, {__call = closer})})
                    local ok, message = pcall(function()
                        for _ in function() end, nil, nil, resource do end
                    end)
                    assert(not ok and string.find(message, "'__call' chain too long", 1, true))
                end
