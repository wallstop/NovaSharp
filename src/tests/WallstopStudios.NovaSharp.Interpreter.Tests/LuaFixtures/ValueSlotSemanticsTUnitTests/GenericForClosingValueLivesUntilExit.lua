-- @lua-versions: 5.1+
-- @novasharp-only: false
-- @expects-error: false
-- @source: src/tests/WallstopStudios.NovaSharp.Interpreter.Tests.TUnit/EndToEnd/ValueSlotSemanticsTUnitTests.cs:132
-- @test: ValueSlotSemanticsTUnitTests.GenericForClosingValueLivesUntilExit
-- Compatibility notes: Test targets Lua 5.4+
local hasClose = _VERSION == 'Lua 5.4' or _VERSION == 'Lua 5.5'
                local closed, closeError
                local closer = setmetatable({}, {__close = function(...)
                    local self, err = ...
                    local expectedCount = _VERSION == 'Lua 5.5' and err == nil and 1 or 2
                    assert(select('#', ...) == expectedCount, 'closing argument count')
                    closed = closed + 1
                    closeError = err
                end})
                local function step(limit, previous)
                    if previous < limit then return previous + 1 end
                end
                local function controls(limit)
                    return step, limit, 0, closer, true
                end
                local function run(mode)
                    if mode == 'iterator error' then
                        local function fail() error('iterator failed', 0) end
                        for _ in fail, nil, nil, closer do error('unreachable') end
                        return
                    end
                    for i in controls(mode == 'empty' and 0 or 2) do
                        assert(closed == 0, 'premature close')
                        if mode == 'break' then break end
                        if mode == 'return' then return i end
                        if mode == 'body error' then error('body failed', 0) end
                    end
                end

                for _, mode in ipairs({'empty', 'exhausted', 'break', 'return', 'body error', 'iterator error'}) do
                    closed, closeError = 0, nil
                    local expectedError = ({['body error'] = 'body failed', ['iterator error'] = 'iterator failed'})[mode]
                    local ok, result = pcall(run, mode)
                    assert(ok == (expectedError == nil), mode)
                    assert(closed == (hasClose and 1 or 0), 'close count: ' .. mode)
                    if mode == 'return' then assert(result == 1) end
                    assert(closeError == (hasClose and expectedError or nil), 'active error: ' .. mode)
                end

                local function checkInvalid()
                    for _ in step, 0, 0, true do error('unreachable') end
                end
                local ok, message = pcall(checkInvalid)
                assert(ok == not hasClose, 'invalid closer')
                if hasClose then
                    assert(string.find(message, "variable '(for state)' got a non-closable value", 1, true))
                end
                for _ in step, 0, 0, false do error('unreachable') end
