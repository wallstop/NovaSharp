-- @lua-versions: 5.4, 5.5
-- @novasharp-only: false
-- @expects-error: false
-- @source: src/tests/WallstopStudios.NovaSharp.Interpreter.Tests.TUnit/Units/CoreLib/ErrorHandlingModuleTUnitTests.cs:848
-- @test: ErrorHandlingModuleTUnitTests.ReentrantCloseAndMessageHandlersSurviveExecutionStackGrowth
-- Compatibility notes: Test targets Lua 5.4+; Lua 5.4+: close attribute
local close_count = 0
                local function recurse(n)
                    if n == 0 then
                        return 0
                    end
                    return 1 + recurse(n - 1)
                end

                local mt = {
                    __close = function()
                        close_count = close_count + 1
                        assert(recurse(96) == 96)
                    end
                }

                local function finish()
                    local handle <close> = setmetatable({}, mt)
                    return 'done'
                end

                local value = finish()
                local handler_count = 0
                local ok, message = xpcall(function()
                    error('boom', 0)
                end, function(err)
                    handler_count = handler_count + 1
                    return 'handled:' .. err .. ':' .. recurse(96)
                end)

                assert(value == 'done')
                assert(close_count == 1)
                assert(ok == false)
                assert(message == 'handled:boom:96')
                assert(handler_count == 1)
                return value, close_count, ok, message, handler_count
