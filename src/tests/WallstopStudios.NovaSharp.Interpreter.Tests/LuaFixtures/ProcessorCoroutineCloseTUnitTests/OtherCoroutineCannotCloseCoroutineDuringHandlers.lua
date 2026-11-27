-- @lua-versions: 5.4, 5.5
-- @novasharp-only: false
-- @expects-error: false
-- @source: src/tests/WallstopStudios.NovaSharp.Interpreter.Tests.TUnit/Units/Execution/ProcessorExecution/ProcessorCoroutineCloseTUnitTests.cs:1200
-- @test: ProcessorCoroutineCloseTUnitTests.OtherCoroutineCannotCloseCoroutineDuringHandlers
-- Compatibility notes: Test targets Lua 5.4+; Lua 5.4+: close attribute
local first, second
                local rejected = false
                second = coroutine.create(function()
                    local ok, err = pcall(function() return coroutine.close(first) end)
                    rejected = ok == false
                        and type(err) == 'string'
                        and err:find('cannot close a normal coroutine', 1, true) ~= nil
                end)

                first = coroutine.create(function()
                    local pending <close> = setmetatable({}, {
                        __close = function()
                            coroutine.resume(second)
                        end
                    })
                    coroutine.yield('pause')
                end)

                local resumed, paused = coroutine.resume(first)
                local closed, closeError = coroutine.close(first)

                print('resume', resumed, paused)
                print('close', closed, closeError, rejected)

                return resumed, paused, closed, closeError, rejected
