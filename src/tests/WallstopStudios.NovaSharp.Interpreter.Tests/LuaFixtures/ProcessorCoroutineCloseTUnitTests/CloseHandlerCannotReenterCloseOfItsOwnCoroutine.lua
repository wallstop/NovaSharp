-- @lua-versions: 5.4
-- @novasharp-only: false
-- @expects-error: false
-- @source: src/tests/WallstopStudios.NovaSharp.Interpreter.Tests.TUnit/Units/Execution/ProcessorExecution/ProcessorCoroutineCloseTUnitTests.cs:1297
-- @test: ProcessorCoroutineCloseTUnitTests.CloseHandlerCannotReenterCloseOfItsOwnCoroutine
-- Compatibility notes: Test targets Lua 5.4+; Lua 5.4+: close attribute
local task
                task = coroutine.create(function()
                    local pending <close> = setmetatable({}, {
                        __close = function()
                            coroutine.close(task)
                        end
                    })
                    coroutine.yield('pause')
                end)

                local resumed, paused = coroutine.resume(task)
                local closed, closeError = coroutine.close(task)
                local rejected = type(closeError) == 'string'
                    and closeError:find('cannot close a running coroutine', 1, true) ~= nil
                local closedAgain, closeErrorAgain = coroutine.close(task)

                print('resume', resumed, paused)
                print('close', closed, rejected)
                print('close2', closedAgain, closeErrorAgain)

                return resumed, paused, closed, rejected, closedAgain, closeErrorAgain
