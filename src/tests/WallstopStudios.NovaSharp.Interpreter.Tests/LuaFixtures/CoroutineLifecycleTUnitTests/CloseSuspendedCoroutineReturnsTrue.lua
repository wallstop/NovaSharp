-- @lua-versions: 5.4, 5.5
-- @novasharp-only: false
-- @expects-error: false
-- @source: src\tests\WallstopStudios.NovaSharp.Interpreter.Tests.TUnit\Units\Execution\ProcessorExecution\CoroutineLifecycleIntegrationTUnitTests.cs:224
-- @test: CoroutineLifecycleTUnitTests.CloseSuspendedCoroutineReturnsTrue
-- @compat-notes: Test targets Lua 5.4+; Lua 5.4: close attribute; Lua 5.3+: bitwise operators
function closable_success()
                    local handle <close> = setmetatable({}, { __close = function() end })
                    coroutine.yield('pause')
                end
