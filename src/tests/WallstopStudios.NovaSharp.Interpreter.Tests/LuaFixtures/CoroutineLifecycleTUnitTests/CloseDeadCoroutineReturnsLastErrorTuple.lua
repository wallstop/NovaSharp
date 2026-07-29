-- @lua-versions: 5.4+
-- @novasharp-only: false
-- @expects-error: false
-- @source: src\tests\WallstopStudios.NovaSharp.Interpreter.Tests.TUnit\Units\Execution\ProcessorExecution\CoroutineLifecycleIntegrationTUnitTests.cs:357
-- @test: CoroutineLifecycleTUnitTests.CloseDeadCoroutineReturnsLastErrorTuple
-- Test targets Lua 5.4+; Lua 5.4+: close attribute
function closable_failure()
                    local handle <close> = setmetatable({}, {
                        __close = function() error('close-dead') end
                    })
                    coroutine.yield()
                end
