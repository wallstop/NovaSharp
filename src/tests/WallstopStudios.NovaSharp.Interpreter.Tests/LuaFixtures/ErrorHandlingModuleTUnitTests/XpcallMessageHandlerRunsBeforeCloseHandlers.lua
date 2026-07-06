-- @lua-versions: 5.4, 5.5
-- @novasharp-only: false
-- @expects-error: false
-- @source: src/tests/WallstopStudios.NovaSharp.Interpreter.Tests.TUnit/Units/CoreLib/ErrorHandlingModuleTUnitTests.cs:719
-- @test: ErrorHandlingModuleTUnitTests.XpcallMessageHandlerRunsBeforeCloseHandlers
-- Compatibility notes: Test targets Lua 5.4+; Lua 5.4+: close attribute
local events = {}
                local mt = {
                    __close = function()
                        events[#events + 1] = 'close'
                    end
                }

                local function inner()
                    local handle <close> = setmetatable({}, mt)
                    error('boom', 0)
                end

                xpcall(function()
                    inner()
                end, function(message)
                    events[#events + 1] = 'handler'
                    return message
                end)

                return table.concat(events, ',')
