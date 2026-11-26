-- @lua-versions: 5.4, 5.5
-- @novasharp-only: false
-- @expects-error: false
-- @source: src/tests/WallstopStudios.NovaSharp.Interpreter.Tests.TUnit/Units/Execution/ProcessorExecution/ProcessorCoroutineCloseTUnitTests.cs:581
-- @test: ProcessorCoroutineCloseTUnitTests.CloseHandlerErrorUnwindsItsOwnScopeBeforeSiblings
-- Compatibility notes: Test targets Lua 5.4+; Lua 5.4+: close attribute
local log = {}
                local function make(name)
                    return setmetatable({}, {
                        __close = function(_, err)
                            log[#log + 1] = name .. ':' .. tostring(err)
                        end
                    })
                end

                local task = coroutine.create(function()
                    local sibling <close> = make('sibling')
                    local failing <close> = setmetatable({}, {
                        __close = function(_, err)
                            log[#log + 1] = 'failing:' .. tostring(err)
                            local inner <close> = make('inner')
                            error('handlerboom', 0)
                        end
                    })
                    error('original', 0)
                end)

                local resumed, resumeError = coroutine.resume(task)
                local closed, closeError = coroutine.close(task)

                print('resume', resumed, resumeError)
                print('close', closed, closeError, #log, log[1], log[2], log[3])

                return resumed, resumeError, closed, closeError,
                    #log, log[1], log[2], log[3]
