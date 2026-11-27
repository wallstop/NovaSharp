-- @lua-versions: 5.4, 5.5
-- @novasharp-only: false
-- @expects-error: false
-- @source: src/tests/WallstopStudios.NovaSharp.Interpreter.Tests.TUnit/Units/Execution/ProcessorExecution/ProcessorCoroutineCloseTUnitTests.cs:1028
-- @test: ProcessorCoroutineCloseTUnitTests.SelfCloseInsideNestedCloserErrorReachesOuterSiblings
-- Compatibility notes: Test targets Lua 5.4+; Lua 5.4+: close attribute
local log = {}
                local task
                task = coroutine.create(function()
                    local outer <close> = setmetatable({}, {
                        __close = function()
                            log[#log + 1] = 'outer-start'
                            local x <close> = setmetatable({}, {
                                __close = function(_, err)
                                    log[#log + 1] = 'x:' .. (err == nil and 'nil' or 'error')
                                end
                            })
                            local b <close> = setmetatable({}, {
                                __close = function(_, err)
                                    log[#log + 1] = 'b:' .. (err == nil and 'nil' or 'error')
                                    do
                                        local a <close> = setmetatable({}, {
                                            __close = function(_, innerError)
                                                log[#log + 1] = 'a:'
                                                    .. (innerError == nil and 'nil' or 'error')
                                                error('aboom', 0)
                                            end
                                        })
                                        local c <close> = setmetatable({}, {
                                            __close = function(_, innerError)
                                                log[#log + 1] = 'c:'
                                                    .. (innerError == nil and 'nil' or 'error')
                                                coroutine.close(task)
                                            end
                                        })
                                    end
                                end
                            })
                        end
                    })
                    coroutine.yield('pause')
                end)

                local resumed, paused = coroutine.resume(task)
                local closed, closeError = coroutine.close(task)

                print('resume', resumed, paused)
                print('close', closed, closeError == nil)
                print('log', table.concat(log, ','))

                return resumed, paused, closed, closeError == nil, table.concat(log, ',')
