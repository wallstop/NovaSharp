-- @lua-versions: 5.4, 5.5
-- @novasharp-only: false
-- @expects-error: false
-- @source: src/tests/WallstopStudios.NovaSharp.Interpreter.Tests.TUnit/Units/CoreLib/ErrorHandlingModuleTUnitTests.cs:832
-- @test: ErrorHandlingModuleTUnitTests.NestedXpcallCloseErrorUsesInnerMessageHandler
-- Compatibility notes: Test targets Lua 5.4+; Lua 5.4+: close attribute; Lua 5.3+: bitwise OR
local events = {}
                local outer_count = 0
                local inner_count = 0
                local mt = {
                    __close = function(_, err)
                        events[#events + 1] = 'close:' .. tostring(err)
                        error('closeerr', 0)
                    end
                }

                local ok, protected_ok, message = xpcall(function()
                    return xpcall(function()
                        local handle <close> = setmetatable({}, mt)
                        error('boom', 0)
                    end, function(message)
                        inner_count = inner_count + 1
                        events[#events + 1] = 'inner:' .. tostring(message)
                        return 'inner:' .. tostring(message)
                    end)
                end, function(message)
                    outer_count = outer_count + 1
                    events[#events + 1] = 'outer:' .. tostring(message)
                    return 'outer:' .. tostring(message)
                end)

                return ok, protected_ok, message, outer_count, inner_count, table.concat(events, '|')
