-- @lua-versions: 5.4, 5.5
-- @novasharp-only: false
-- @expects-error: false
-- @source: src/tests/WallstopStudios.NovaSharp.Interpreter.Tests.TUnit/Modules/CoroutineModuleTUnitTests.cs:1612
-- @test: CoroutineModuleTUnitTests.FormatCallableTableMetamethodErrorDefersLocalCloseUntilCoroutineClose
-- Compatibility notes: Test targets Lua 5.4+; Lua 5.4+: close attribute
local seen
                local callable = setmetatable({}, {
                    __call = function()
                        local pending <close> = setmetatable({}, {
                            __close = function(_, closeError) seen = closeError end
                        })
                        error('format-call', 0)
                    end
                })
                local value = setmetatable({}, { __tostring = callable })
                local task = coroutine.create(function()
                    return string.format('%s', value)
                end)

                local resumed, resumeError = coroutine.resume(task)
                local before = seen
                local closed, closeError = coroutine.close(task)

                return resumed, resumeError, before, closed, closeError, seen
