-- @lua-versions: 5.4, 5.5
-- @novasharp-only: false
-- @expects-error: false
-- @source: src/tests/WallstopStudios.NovaSharp.Interpreter.Tests.TUnit/Units/Execution/ProcessorExecution/ProcessorCoroutineCloseTUnitTests.cs:1850
-- @test: ProcessorCoroutineCloseTUnitTests.LaterOuterSiblingErrorReplacesCarriedSelfCloseError
-- Compatibility notes: Test targets Lua 5.4+; Lua 5.4+: close attribute
local task
                task = coroutine.create(function()
                    local outer <close> = setmetatable({}, {
                        __close = function()
                            local y <close> = setmetatable({}, {
                                __close = function()
                                    error('yboom', 0)
                                end
                            })
                            local x <close> = setmetatable({}, {
                                __close = function()
                                end
                            })
                            local b <close> = setmetatable({}, {
                                __close = function()
                                    do
                                        local a <close> = setmetatable({}, {
                                            __close = function()
                                                error('aboom', 0)
                                            end
                                        })
                                        local c <close> = setmetatable({}, {
                                            __close = function()
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

                local resumed = coroutine.resume(task)
                local closed, closeError = coroutine.close(task)
                return resumed, closed, closeError
