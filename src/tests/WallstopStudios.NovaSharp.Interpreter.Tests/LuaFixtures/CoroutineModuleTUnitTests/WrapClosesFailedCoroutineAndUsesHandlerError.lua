-- @lua-versions: 5.4, 5.5
-- @novasharp-only: false
-- @expects-error: false
-- @source: src/tests/WallstopStudios.NovaSharp.Interpreter.Tests.TUnit/Modules/CoroutineModuleTUnitTests.cs:1266
-- @test: CoroutineModuleTUnitTests.WrapClosesFailedCoroutineAndUsesHandlerError
-- Compatibility notes: Test targets Lua 5.4+; Lua 5.4+: close attribute
local log = {}
                local wrapped = coroutine.wrap(function()
                    local pending <close> = setmetatable({}, {
                        __close = function(_, err)
                            log[#log + 1] = 'first:' .. tostring(err)
                        end
                    })
                    error('boom', 0)
                end)

                local ok, err = pcall(wrapped)

                local replacement = coroutine.wrap(function()
                    local pending <close> = setmetatable({}, {
                        __close = function(_, err)
                            log[#log + 1] = 'second:' .. tostring(err)
                            error('closeboom', 0)
                        end
                    })
                    error('original', 0)
                end)

                local ok2, err2 = pcall(replacement)

                print('first', ok, err, #log, log[1])
                print('second', ok2, err2, #log, log[2])

                return ok, err, ok2, err2, #log, log[1], log[2]
