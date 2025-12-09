-- @lua-versions: 5.4, 5.5
-- @novasharp-only: false
-- @expects-error: false
-- @source: src/tests/WallstopStudios.NovaSharp.Interpreter.Tests.TUnit/Units/Execution/ProcessorExecution/CoroutineLifecycleIntegrationTUnitTests.cs:295
-- @test: CoroutineLifecycleTUnitTests.CloseDeadCoroutineReturnsLastErrorTuple
-- @compat-notes: Lua 5.4: close attribute; Lua 5.3+: bitwise operators
function closable_failure()
                    local handle <close> = setmetatable({}, {
                        __close = function() error('close-dead') end
                    })
                    coroutine.yield()
                end
