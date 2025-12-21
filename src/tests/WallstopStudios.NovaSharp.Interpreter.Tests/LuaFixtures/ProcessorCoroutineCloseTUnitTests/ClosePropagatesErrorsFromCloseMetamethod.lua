-- @lua-versions: 5.4, 5.5
-- @novasharp-only: false
-- @expects-error: false
-- @source: src/tests/WallstopStudios.NovaSharp.Interpreter.Tests.TUnit/Units/Execution/ProcessorExecution/ProcessorCoroutineCloseTUnitTests.cs:327
-- @test: ProcessorCoroutineCloseTUnitTests.ClosePropagatesErrorsFromCloseMetamethod
-- @compat-notes: Test targets Lua 5.4+; Lua 5.4: close attribute
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
