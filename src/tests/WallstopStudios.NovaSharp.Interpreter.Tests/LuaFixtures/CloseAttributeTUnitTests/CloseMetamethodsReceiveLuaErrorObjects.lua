-- @lua-versions: 5.4, 5.5
-- @novasharp-only: false
-- @expects-error: false
-- @source: src/tests/WallstopStudios.NovaSharp.Interpreter.Tests.TUnit/Units/Execution/CloseAttributeTUnitTests.cs:236
-- @test: CloseAttributeTUnitTests.CloseMetamethodsReceiveLuaErrorObjects
-- Compatibility notes: Test targets Lua 5.4+; Lua 5.4+: close attribute
local original, replacement = {}, {}
                local captured, replacedCaptured

                local ok, caught = pcall(function()
                    local value <close> = setmetatable({}, {
                        __close = function(_, err) captured = err end
                    })
                    error(original)
                end)

                local ok2, caught2 = pcall(function()
                    local first <close> = setmetatable({}, {
                        __close = function(_, err) replacedCaptured = err end
                    })
                    local second <close> = setmetatable({}, {
                        __close = function() error(replacement) end
                    })
                    error(original)
                end)

                local argCount, nilArgType = 0, 'none'
                local ok3, caught3 = pcall(function()
                    local value <close> = setmetatable({}, {
                        __close = function(...)
                            argCount = select('#', ...)
                            nilArgType = type((select(2, ...)))
                        end
                    })
                    error(nil)
                end)

                print('table', ok, rawequal(original, caught), rawequal(original, captured))
                print('replacement', ok2, rawequal(replacement, caught2), rawequal(replacement, replacedCaptured))
                print('nil', ok3, argCount, nilArgType, type(caught3), caught3)

                return
                    ok, rawequal(original, caught), rawequal(original, captured),
                    ok2, rawequal(replacement, caught2), rawequal(replacement, replacedCaptured),
                    ok3, argCount, nilArgType, type(caught3), caught3
