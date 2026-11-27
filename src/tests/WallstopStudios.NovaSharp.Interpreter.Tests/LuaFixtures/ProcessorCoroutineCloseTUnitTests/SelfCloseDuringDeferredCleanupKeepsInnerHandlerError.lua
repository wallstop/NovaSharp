-- @lua-versions: 5.5
-- @novasharp-only: false
-- @expects-error: false
-- @source: src/tests/WallstopStudios.NovaSharp.Interpreter.Tests.TUnit/Units/Execution/ProcessorExecution/ProcessorCoroutineCloseTUnitTests.cs:685
-- @test: ProcessorCoroutineCloseTUnitTests.SelfCloseDuringDeferredCleanupKeepsInnerHandlerError
-- Compatibility notes: Test targets Lua 5.5+; Lua 5.4+: close attribute
local log = {}
                local task
                task = coroutine.create(function()
                    local failing <close> = setmetatable({}, {
                        __close = function(_, err)
                            log[#log + 1] = 'failing:' .. tostring(err)
                            local inner <close> = setmetatable({}, {
                                __close = function(_, innerError)
                                    log[#log + 1] = 'inner:' .. tostring(innerError)
                                    error('innerboom', 0)
                                end
                            })
                            coroutine.close(task)
                        end
                    })
                    error('original', 0)
                end)

                local resumed, resumeError = coroutine.resume(task)
                local closed, closeError = coroutine.close(task)

                print('resume', resumed, resumeError)
                print('close', closed, closeError, #log, log[1], log[2])

                return resumed, resumeError, closed, closeError,
                    #log, log[1], log[2]
