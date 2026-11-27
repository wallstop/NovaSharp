-- @lua-versions: 5.5
-- @novasharp-only: false
-- @expects-error: false
-- @source: src/tests/WallstopStudios.NovaSharp.Interpreter.Tests.TUnit/Units/Execution/ProcessorExecution/ProcessorCoroutineCloseTUnitTests.cs:1438
-- @test: ProcessorCoroutineCloseTUnitTests.SelfCloseClearsPriorHandlerErrorBeforeSiblings
-- Compatibility notes: Test targets Lua 5.5+; Lua 5.4+: close attribute
local log = {}
                local task
                task = coroutine.create(function()
                    local first <close> = setmetatable({}, {
                        __close = function(_, err)
                            log[#log + 1] = 'first:' .. tostring(err)
                            error('firstboom', 0)
                        end
                    })
                    local second <close> = setmetatable({}, {
                        __close = function(_, err)
                            log[#log + 1] = 'second:' .. tostring(err)
                            coroutine.close(task)
                        end
                    })
                    coroutine.yield('pause')
                end)

                local resumed, paused = coroutine.resume(task)
                local closed, closeError = coroutine.close(task)

                print('resume', resumed, paused)
                print('close', closed, closeError, #log, log[1], log[2])

                return resumed, paused, closed, closeError, #log, log[1], log[2]
