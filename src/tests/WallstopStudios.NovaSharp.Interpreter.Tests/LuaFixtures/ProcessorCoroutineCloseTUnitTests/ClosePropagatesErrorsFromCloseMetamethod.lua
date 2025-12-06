-- @lua-versions: 5.4
-- @novasharp-only: false
-- @expects-error: false
-- @source: src/tests/NovaSharp.Interpreter.Tests.TUnit/VM/ProcessorCoroutineCloseTUnitTests.cs:261
-- @test: ProcessorCoroutineCloseTUnitTests.ClosePropagatesErrorsFromCloseMetamethod
-- @compat-notes: Lua 5.4: close attribute; Lua 5.3+: bitwise operators
local function new_closable()
                    local resource = {}
                    return setmetatable(resource, {
                        __close = function(_, err)
                            error('close failure', 0)
                        end
                    })
                end

                function build_closer_coroutine()
                    return coroutine.create(function()
                        local resource <close> = new_closable()
                        coroutine.yield('pause')
                    end)
                end
