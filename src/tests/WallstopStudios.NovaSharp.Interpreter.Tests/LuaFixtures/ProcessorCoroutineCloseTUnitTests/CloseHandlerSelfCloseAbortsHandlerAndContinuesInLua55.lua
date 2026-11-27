-- @lua-versions: 5.5
-- @novasharp-only: false
-- @expects-error: false
-- @source: src/tests/WallstopStudios.NovaSharp.Interpreter.Tests.TUnit/Units/Execution/ProcessorExecution/ProcessorCoroutineCloseTUnitTests.cs:1433
-- @test: ProcessorCoroutineCloseTUnitTests.CloseHandlerSelfCloseAbortsHandlerAndContinuesInLua55
-- Compatibility notes: Test targets Lua 5.5+; Lua 5.4+: close attribute
local log = {}
                local task
                task = coroutine.create(function()
                    local first <close> = setmetatable({}, {
                        __close = function()
                            log[#log + 1] = 'first'
                            local inner <close> = setmetatable({}, {
                                __close = function(_, err)
                                    log[#log + 1] = 'inner:' .. tostring(err)
                                end
                            })
                            coroutine.close(task)
                            log[#log + 1] = 'after'
                        end
                    })
                    local second <close> = setmetatable({}, {
                        __close = function()
                            log[#log + 1] = 'second'
                        end
                    })
                    coroutine.yield('pause')
                end)

                local resumed, paused = coroutine.resume(task)
                local closed, closeError = coroutine.close(task)

                print('resume', resumed, paused)
                print('close', closed, closeError, #log, log[1], log[2], log[3])

                return resumed, paused, closed, closeError,
                    #log, log[1], log[2], log[3]
