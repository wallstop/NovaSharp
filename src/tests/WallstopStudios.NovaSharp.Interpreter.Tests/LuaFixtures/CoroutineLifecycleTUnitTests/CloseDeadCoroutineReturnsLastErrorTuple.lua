-- @lua-versions: 5.4
-- @novasharp-only: false
-- @expects-error: false
-- @source: src/tests/NovaSharp.Interpreter.Tests.TUnit/VM/CoroutineLifecycleTUnitTests.cs:295
-- @test: CoroutineLifecycleTUnitTests.CloseDeadCoroutineReturnsLastErrorTuple
-- @compat-notes: Lua 5.4: close attribute; Lua 5.3+: bitwise operators
function closable_failure()
                    local handle <close> = setmetatable({}, {
                        __close = function() error('close-dead') end
                    })
                    coroutine.yield()
                end
