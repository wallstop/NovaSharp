-- @lua-versions: 5.4, 5.5
-- @novasharp-only: false
-- @expects-error: false
-- @source: src/tests/WallstopStudios.NovaSharp.Interpreter.Tests.TUnit/Units/DataTypes/CoroutineLifecycleTUnitTests.cs:282
-- @test: CoroutineLifecycleTUnitTests.CloseSuspendedCoroutineReturnsTrue
-- Compatibility notes: Test targets Lua 5.4+; Lua 5.4+: close attribute
function closable_success()
                    local handle <close> = setmetatable({}, { __close = function() end })
                    coroutine.yield('pause')
                end
