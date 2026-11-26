-- @lua-versions: 5.4, 5.5
-- @novasharp-only: false
-- @expects-error: false
-- @source: src/tests/WallstopStudios.NovaSharp.Interpreter.Tests.TUnit/Units/Execution/ProcessorExecution/ProcessorCoroutineCloseTUnitTests.cs:224
-- @test: ProcessorCoroutineCloseTUnitTests.DeferredCloseHandlerErrorReplacesActiveErrorAndClosesOnce
-- Compatibility notes: Test targets Lua 5.4+; Lua 5.4+: close attribute
local log = {}
                local task = coroutine.create(function()
                    local outer <close> = setmetatable({}, {
                        __close = function(_, err)
                            log[#log + 1] = 'outer:' .. tostring(err)
                        end
                    })
                    local inner <close> = setmetatable({}, {
                        __close = function(_, err)
                            log[#log + 1] = 'inner:' .. tostring(err)
                            error('closeboom', 0)
                        end
                    })
                    error('original', 0)
                end)

                local ok, err = coroutine.resume(task)
                local closed, closeError = coroutine.close(task)
                local closedAgain, closeErrorAgain = coroutine.close(task)

                print('resume', ok, err)
                print('close', closed, closeError, #log, log[1], log[2])
                print('close2', closedAgain, closeErrorAgain)

                return ok, err, closed, closeError,
                    #log, log[1], log[2], closedAgain, closeErrorAgain
