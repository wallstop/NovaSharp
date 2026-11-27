-- @lua-versions: 5.4, 5.5
-- @novasharp-only: false
-- @expects-error: false
-- @source: src/tests/WallstopStudios.NovaSharp.Interpreter.Tests.TUnit/Units/Execution/ProcessorExecution/ProcessorCoroutineCloseTUnitTests.cs:1565
-- @test: ProcessorCoroutineCloseTUnitTests.CloseHandlerObservesRunningCoroutineState
-- Compatibility notes: Test targets Lua 5.4+; Lua 5.4+: close attribute
local observedStatus, observedYieldable
                local task
                task = coroutine.create(function()
                    local pending <close> = setmetatable({}, {
                        __close = function()
                            observedStatus = coroutine.status(task)
                            observedYieldable = coroutine.isyieldable()
                        end
                    })
                    error('boom', 0)
                end)

                local resumed = coroutine.resume(task)
                local closed = coroutine.close(task)

                print('states', resumed, closed, observedStatus, observedYieldable)

                return resumed, closed, observedStatus, observedYieldable
