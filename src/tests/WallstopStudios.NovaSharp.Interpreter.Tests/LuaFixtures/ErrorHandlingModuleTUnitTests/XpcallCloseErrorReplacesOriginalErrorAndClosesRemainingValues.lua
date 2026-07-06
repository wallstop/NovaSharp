-- @lua-versions: 5.4, 5.5
-- @novasharp-only: false
-- @expects-error: false
-- @source: src/tests/WallstopStudios.NovaSharp.Interpreter.Tests.TUnit/Units/CoreLib/ErrorHandlingModuleTUnitTests.cs:780
-- @test: ErrorHandlingModuleTUnitTests.XpcallCloseErrorReplacesOriginalErrorAndClosesRemainingValues
-- Compatibility notes: Test targets Lua 5.4+; Lua 5.4+: close attribute; Lua 5.3+: bitwise OR
local events = {}
                local first_mt = {
                    __close = function(_, err)
                        events[#events + 1] = 'first:' .. tostring(err)
                    end
                }
                local throwing_mt = {
                    __close = function(_, err)
                        events[#events + 1] = 'throwing:' .. tostring(err)
                        error('closeerr', 0)
                    end
                }
                local last_mt = {
                    __close = function(_, err)
                        events[#events + 1] = 'last:' .. tostring(err)
                    end
                }

                local ok, message = xpcall(function()
                    local first <close> = setmetatable({}, first_mt)
                    local throwing <close> = setmetatable({}, throwing_mt)
                    local last <close> = setmetatable({}, last_mt)
                    error('boom', 0)
                end, function(message)
                    events[#events + 1] = 'handler:' .. tostring(message)
                    return 'handled:' .. tostring(message)
                end)

                return ok, message, table.concat(events, '|')
