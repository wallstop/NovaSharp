-- @lua-versions: 5.4, 5.5
-- @novasharp-only: false
-- @expects-error: false
-- @source: src/tests/WallstopStudios.NovaSharp.Interpreter.Tests.TUnit/Units/Execution/ProcessorExecution/ProcessorCoroutineCloseTUnitTests.cs:1743
-- @test: ProcessorCoroutineCloseTUnitTests.FailedResumeKeepsDeferredCloseError
-- Compatibility notes: Test targets Lua 5.4+; Lua 5.4+: close attribute
local original = {}
                local seen
                local task = coroutine.create(function()
                    local pending <close> = setmetatable({}, {
                        __close = function(_, err) seen = err end
                    })
                    error(original)
                end)

                local resumed, resumeError = coroutine.resume(task)
                local resumedAgain, resumeErrorAgain = coroutine.resume(task)
                local closed, closeError = coroutine.close(task)

                print('resume', resumed, rawequal(original, resumeError))
                print('retry', resumedAgain, type(resumeErrorAgain))
                print('close', closed, rawequal(original, seen), rawequal(original, closeError))

                return resumed, rawequal(original, resumeError),
                    resumedAgain, type(resumeErrorAgain),
                    closed, rawequal(original, seen), rawequal(original, closeError)
