-- @lua-versions: 5.4, 5.5
-- @novasharp-only: false
-- @expects-error: false
-- @source: src/tests/WallstopStudios.NovaSharp.Interpreter.Tests.TUnit/Units/Execution/ProcessorExecution/ProcessorCoroutineCloseTUnitTests.cs:1907
-- @test: ProcessorCoroutineCloseTUnitTests.TailCallErrorDefersCloseHandlersUntilCoroutineClose
-- Compatibility notes: Test targets Lua 5.4+; Lua 5.4+: close attribute
local log = {}
                local function fail()
                    local pending <close> = setmetatable({}, {
                        __close = function(_, err)
                            log[#log + 1] = 'closed:' .. tostring(err)
                        end
                    })
                    error('boom', 0)
                end

                local task = coroutine.create(function()
                    return fail()
                end)

                local resumed, resumeError = coroutine.resume(task)
                local beforeClose = #log
                local closed, closeError = coroutine.close(task)

                print(resumed, resumeError, beforeClose, closed, closeError, #log, log[1])
                return resumed, resumeError, beforeClose, closed, closeError, #log, log[1]
