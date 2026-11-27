-- @lua-versions: 5.4, 5.5
-- @novasharp-only: false
-- @expects-error: false
-- @source: src/tests/WallstopStudios.NovaSharp.Interpreter.Tests.TUnit/Units/Execution/ProcessorExecution/ProcessorCoroutineCloseTUnitTests.cs:525
-- @test: ProcessorCoroutineCloseTUnitTests.SuspendedCloseContinuesThroughOuterCloseHandlers
-- Compatibility notes: Test targets Lua 5.4+; Lua 5.4+: close attribute
local log = {}
                local function make(name, shouldError)
                    return setmetatable({}, {
                        __close = function(_, err)
                            log[#log + 1] = name .. ':' .. tostring(err)
                            if shouldError then
                                error('close:' .. name, 0)
                            end
                        end
                    })
                end

                local function inner()
                    local pending <close> = make('inner', true)
                    coroutine.yield('pause')
                end

                local task = coroutine.create(function()
                    local outer <close> = make('outer', false)
                    inner()
                end)

                local resumed, paused = coroutine.resume(task)
                local closed, closeError = coroutine.close(task)
                local closedAgain, closeErrorAgain = coroutine.close(task)

                print('resume', resumed, paused)
                print('close', closed, closeError, #log, log[1], log[2])
                print('close2', closedAgain, closeErrorAgain, #log)

                return resumed, paused, closed, closeError,
                    #log, log[1], log[2], closedAgain, closeErrorAgain
