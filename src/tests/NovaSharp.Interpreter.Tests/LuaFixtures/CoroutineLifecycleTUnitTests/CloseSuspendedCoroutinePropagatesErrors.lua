-- @lua-versions: 5.4
-- @novasharp-only: false
-- @expects-error: false
-- @source: src/tests/NovaSharp.Interpreter.Tests.TUnit/VM/CoroutineLifecycleTUnitTests.cs:251
-- @test: CoroutineLifecycleTUnitTests.CloseSuspendedCoroutinePropagatesErrors
-- @compat-notes: Test targets Lua 5.4+; Lua 5.4: close attribute; Lua 5.3+: bitwise operators
function closable_failure()
                    local handle <close> = setmetatable({}, {
                        __close = function() error('close-fail') end
                    })
                    coroutine.yield('pause')
                end
