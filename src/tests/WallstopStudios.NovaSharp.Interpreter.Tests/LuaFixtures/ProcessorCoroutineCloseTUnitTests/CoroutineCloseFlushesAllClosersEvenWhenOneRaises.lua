-- @lua-versions: 5.4, 5.5
-- @novasharp-only: false
-- @expects-error: false
-- @source: src/tests/WallstopStudios.NovaSharp.Interpreter.Tests.TUnit/Units/Execution/ProcessorExecution/ProcessorCoroutineCloseTUnitTests.cs:295
-- @test: ProcessorCoroutineCloseTUnitTests.CoroutineCloseFlushesAllClosersEvenWhenOneRaises
-- @compat-notes: Lua 5.4: close attribute; Lua 5.3+: bitwise operators
local log = {}

                local function new_closable(name, should_error)
                    local token = {}
                    setmetatable(token, {
                        __close = function(_, err)
                            table.insert(log, string.format('%s:%s', name, err or 'nil'))
                            if should_error then
                                error('close:' .. name, 0)
                            end
                        end
                    })
                    return token
                end

                function build_pending_close_coroutine()
                    return coroutine.create(function()
                        local first <close> = new_closable('first', true)
                        local second <close> = new_closable('second', false)
                        coroutine.yield('pause')
                    end)
                end

                function read_close_log()
                    return log
                end
