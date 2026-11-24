-- @lua-versions: novasharp-only
-- @novasharp-only: true
-- @expects-error: false
-- @source: src/tests/WallstopStudios.NovaSharp.Interpreter.Tests.TUnit/EndToEnd/ValueSlotSemanticsTUnitTests.cs:291
-- @test: ValueSlotSemanticsTUnitTests.GenericForNativeCloseRunsOnActiveCoroutine
-- Compatibility notes: Test targets Lua 5.4+
local nested = setmetatable({}, {__call = nativeCloser})
                local indirect = setmetatable({}, {__call = nested})
                local function done() end
                local function check(closer, expectedCount)
                    local thread = coroutine.create(function()
                        local value = setmetatable({}, {__close = closer})
                        for _ in done, nil, nil, value do end
                    end)
                    local ok, err = coroutine.resume(thread)
                    assert(ok, err)
                    assert(closeThread == thread and closeArgumentCount == expectedCount)
                end
                check(nativeCloser, 2)
                check(indirect, 4)
