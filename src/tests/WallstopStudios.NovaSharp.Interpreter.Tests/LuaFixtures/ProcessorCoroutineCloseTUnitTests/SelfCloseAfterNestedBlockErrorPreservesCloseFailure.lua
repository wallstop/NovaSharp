-- @lua-versions: 5.4, 5.5
-- @novasharp-only: false
-- @expects-error: false
-- @source: src/tests/WallstopStudios.NovaSharp.Interpreter.Tests.TUnit/Units/Execution/ProcessorExecution/ProcessorCoroutineCloseTUnitTests.cs:790
-- @test: ProcessorCoroutineCloseTUnitTests.SelfCloseAfterNestedBlockErrorPreservesCloseFailure
-- Compatibility notes: Test targets Lua 5.4+; Lua 5.4+: close attribute
local log = {}
                local task
                task = coroutine.create(function()
                    local outer <close> = setmetatable({}, {
                        __close = function(_, err)
                            log[#log + 1] = 'outer:' .. (err == nil and 'nil' or 'error')
                            coroutine.close(task)
                        end
                    })
                    do
                        local inner <close> = setmetatable({}, {
                            __close = function(_, err)
                                log[#log + 1] = 'inner:' .. (err == nil and 'nil' or 'error')
                                error('innerboom', 0)
                            end
                        })
                        coroutine.yield('pause')
                    end
                end)

                local resumed, paused = coroutine.resume(task)
                local closed, closeError = coroutine.close(task)

                print('resume', resumed, paused)
                print('close', closed, closeError == nil)
                print('log', table.concat(log, ','))

                return resumed, paused, closed, closeError == nil, table.concat(log, ',')
