-- @lua-versions: 5.4, 5.5
-- @novasharp-only: false
-- @expects-error: false
-- @source: src/tests/WallstopStudios.NovaSharp.Interpreter.Tests.TUnit/Units/Execution/ProcessorExecution/ProcessorCoroutineCloseTUnitTests.cs:839
-- @test: ProcessorCoroutineCloseTUnitTests.CloseErrorInsideProtectedCallDoesNotFailClose
-- Compatibility notes: Test targets Lua 5.4+; Lua 5.4+: close attribute
local log = {}
                local task
                task = coroutine.create(function()
                    local outer <close> = setmetatable({}, {
                        __close = function()
                            log[#log + 1] = 'outer-start'
                            local ok, err = pcall(function()
                                local inner <close> = setmetatable({}, {
                                    __close = function()
                                        log[#log + 1] = 'inner'
                                        error('innerboom', 0)
                                    end
                                })
                            end)
                            log[#log + 1] = 'pcall:' .. tostring(ok) .. ':' .. tostring(err)
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
