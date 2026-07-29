-- @lua-versions: 5.4, 5.5
-- @novasharp-only: false
-- @expects-error: false
-- @source: src/tests/WallstopStudios.NovaSharp.Interpreter.Tests.TUnit/Units/DataTypes/CoroutineLifecycleTUnitTests.cs:315
-- @test: CoroutineLifecycleTUnitTests.CloseSuspendedCoroutinePropagatesErrors
-- Compatibility notes: Test targets Lua 5.4+; Lua 5.4+: close attribute
function closable_failure()
                    local handle <close> = setmetatable({}, {
                        __close = function() error('close-fail') end
                    })
                    coroutine.yield('pause')
                end
