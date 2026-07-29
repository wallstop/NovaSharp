-- @lua-versions: none
-- @novasharp-only: false
-- @expects-error: false
-- @source: src/tests/WallstopStudios.NovaSharp.Interpreter.Tests.TUnit/Units/DataTypes/CoroutineLifecycleTUnitTests.cs:315
-- @test: CoroutineLifecycleTUnitTests.CloseSuspendedCoroutinePropagatesErrors
-- Test targets Lua 5.1; Lua 5.4+: close attribute
function closable_failure()
                    local handle <close> = setmetatable({}, {
                        __close = function() error('close-fail') end
                    })
                    coroutine.yield('pause')
                end
