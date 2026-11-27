-- @lua-versions: 5.5
-- @novasharp-only: false
-- @expects-error: false
-- @source: src/tests/WallstopStudios.NovaSharp.Interpreter.Tests.TUnit/Units/Execution/ProcessorExecution/ProcessorCoroutineCloseTUnitTests.cs:685
-- @test: ProcessorCoroutineCloseTUnitTests.SelfCloseInsideNestedBlockDoesNotResumeRemovedFrame
-- Compatibility notes: Test targets Lua 5.5+; Lua 5.4+: close attribute
local log = {}
                local task
                task = coroutine.create(function()
                    local outer <close> = setmetatable({}, {
                        __close = function()
                            log[#log + 1] = 'outer-start'
                            do
                                local inner <close> = setmetatable({}, {
                                    __close = function()
                                        log[#log + 1] = 'inner'
                                        coroutine.close(task)
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
                print('close', closed, closeError, #log, log[1], log[2])

                return resumed, paused, closed, closeError, #log, log[1], log[2]
