-- @lua-versions: 5.4, 5.5
-- @novasharp-only: false
-- @expects-error: true
-- @source: src/tests/WallstopStudios.NovaSharp.Interpreter.Tests.TUnit/Units/DataTypes/CoroutineLifecycleTUnitTests.cs:128
-- @test: CoroutineLifecycleTUnitTests.RecycleCoroutineThrowsWhileCloseHandlersArePending
-- Compatibility notes: Test targets Lua 5.4+; Lua 5.4+: close attribute
function failing()
                    local pending <close> = setmetatable({}, {
                        __close = function() error('close failure') end
                    })
                    error('failure')
                end

                function replacement()
                    return 'recycled'
                end
