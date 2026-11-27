-- @lua-versions: 5.4, 5.5
-- @novasharp-only: false
-- @expects-error: false
-- @source: src/tests/WallstopStudios.NovaSharp.Interpreter.Tests.TUnit/Modules/CoroutineModuleTUnitTests.cs:1535
-- @test: CoroutineModuleTUnitTests.SortComparatorErrorDefersLocalCloseUntilCoroutineClose
-- Compatibility notes: Test targets Lua 5.4+; Lua 5.4+: close attribute
local seen
                local task = coroutine.create(function()
                    table.sort({ 3, 1, 2 }, function()
                        local pending <close> = setmetatable({}, {
                            __close = function(_, closeError) seen = closeError end
                        })
                        error('cmp', 0)
                    end)
                end)

                local resumed, resumeError = coroutine.resume(task)
                local before = seen
                local closed, closeError = coroutine.close(task)

                print('resume', resumed, resumeError, before)
                print('close', closed, closeError, seen)

                return resumed, resumeError, before, closed, closeError, seen
