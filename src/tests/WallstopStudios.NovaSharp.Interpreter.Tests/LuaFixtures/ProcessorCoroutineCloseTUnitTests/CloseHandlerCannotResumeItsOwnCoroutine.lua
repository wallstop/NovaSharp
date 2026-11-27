-- @lua-versions: 5.4, 5.5
-- @novasharp-only: false
-- @expects-error: false
-- @source: src/tests/WallstopStudios.NovaSharp.Interpreter.Tests.TUnit/Units/Execution/ProcessorExecution/ProcessorCoroutineCloseTUnitTests.cs:1387
-- @test: ProcessorCoroutineCloseTUnitTests.CloseHandlerCannotResumeItsOwnCoroutine
-- Compatibility notes: Test targets Lua 5.4+; Lua 5.4+: close attribute
local observed
                local task
                task = coroutine.create(function()
                    local pending <close> = setmetatable({}, {
                        __close = function()
                            local resumed, value, message = pcall(coroutine.resume, task)
                            observed = tostring(resumed) .. ':' .. tostring(value) .. ':' .. tostring(message)
                        end
                    })
                    coroutine.yield('pause')
                end)

                local resumed, paused = coroutine.resume(task)
                local closed, closeError = coroutine.close(task)

                print('resume', resumed, paused)
                print('close', closed, closeError, observed)

                return resumed, paused, closed, closeError, observed
