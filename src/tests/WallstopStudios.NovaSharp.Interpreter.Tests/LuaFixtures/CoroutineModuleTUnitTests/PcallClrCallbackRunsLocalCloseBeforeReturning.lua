-- @lua-versions: 5.4, 5.5
-- @novasharp-only: false
-- @expects-error: false
-- @source: src/tests/WallstopStudios.NovaSharp.Interpreter.Tests.TUnit/Modules/CoroutineModuleTUnitTests.cs:1420
-- @test: CoroutineModuleTUnitTests.PcallClrCallbackRunsLocalCloseBeforeReturning
-- Compatibility notes: Test targets Lua 5.4+; Lua 5.4+: close attribute
local seen
                local task = coroutine.create(function()
                    local ok, err = pcall(string.gsub, 'abc', 'b', function()
                        local pending <close> = setmetatable({}, {
                            __close = function(_, closeError) seen = closeError end
                        })
                        error('rep', 0)
                    end)
                    return ok, err, seen
                end)

                local resumed, ok, err, observed = coroutine.resume(task)

                print('resume', resumed, ok, err, observed)

                return resumed, ok, err, observed
