-- @lua-versions: 5.4, 5.5
-- @novasharp-only: false
-- @expects-error: false
-- @source: src/tests/WallstopStudios.NovaSharp.Interpreter.Tests.TUnit/Units/Execution/ProcessorExecution/ProcessorCoroutineCloseTUnitTests.cs:173
-- @test: ProcessorCoroutineCloseTUnitTests.UncaughtErrorDefersCloseHandlersUntilClose
-- Compatibility notes: Test targets Lua 5.4+; Lua 5.4+: close attribute
local seen = {}
                local function make(name)
                    return setmetatable({}, {
                        __close = function(_, err)
                            seen[#seen + 1] = name .. ':' .. tostring(err)
                        end
                    })
                end

                local task = coroutine.create(function()
                    local explicit <close> = make('explicit')
                    for _ in function() error('boom', 0) end, nil, nil, make('generic') do end
                end)

                local ok, err = coroutine.resume(task)
                local before = #seen
                local closed, closeError = coroutine.close(task)
                local closedAgain, closeErrorAgain = coroutine.close(task)

                print('resume', ok, err, before)
                print('close', closed, closeError, #seen, seen[1], seen[2])
                print('close2', closedAgain, closeErrorAgain, #seen)

                return ok, err, before,
                    closed, closeError, #seen, seen[1], seen[2],
                    closedAgain, closeErrorAgain
