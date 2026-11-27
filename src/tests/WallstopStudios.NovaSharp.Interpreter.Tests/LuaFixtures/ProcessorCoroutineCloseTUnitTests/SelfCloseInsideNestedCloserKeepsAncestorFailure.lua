-- @lua-versions: 5.4, 5.5
-- @novasharp-only: false
-- @expects-error: false
-- @source: src/tests/WallstopStudios.NovaSharp.Interpreter.Tests.TUnit/Units/Execution/ProcessorExecution/ProcessorCoroutineCloseTUnitTests.cs:1171
-- @test: ProcessorCoroutineCloseTUnitTests.SelfCloseInsideNestedCloserKeepsAncestorFailure
-- Compatibility notes: Test targets Lua 5.4+; Lua 5.4+: close attribute
local log = {}
                local task
                task = coroutine.create(function()
                    local outer <close> = setmetatable({}, {
                        __close = function()
                            log[#log + 1] = 'outer-start'
                            do
                                local a <close> = setmetatable({}, {
                                    __close = function(_, err)
                                        log[#log + 1] = 'a:' .. (err == nil and 'nil' or 'error')
                                    end
                                })
                                local b <close> = setmetatable({}, {
                                    __close = function(_, err)
                                        log[#log + 1] = 'b:' .. (err == nil and 'nil' or 'error')
                                        local nested <close> = setmetatable({}, {
                                            __close = function(_, nestedError)
                                                log[#log + 1] = 'nested:'
                                                    .. (nestedError == nil and 'nil' or 'error')
                                                coroutine.close(task)
                                            end
                                        })
                                        log[#log + 1] = 'b-after'
                                    end
                                })
                                local c <close> = setmetatable({}, {
                                    __close = function(_, err)
                                        log[#log + 1] = 'c:' .. (err == nil and 'nil' or 'error')
                                        error('cboom', 0)
                                    end
                                })
                            end
                            log[#log + 1] = 'outer-after'
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
