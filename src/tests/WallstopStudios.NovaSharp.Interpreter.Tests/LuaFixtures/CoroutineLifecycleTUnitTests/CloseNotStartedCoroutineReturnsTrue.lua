-- @lua-versions: 5.1
-- @novasharp-only: false
-- @expects-error: false
-- @source: src/tests/WallstopStudios.NovaSharp.Interpreter.Tests.TUnit/Units/DataTypes/CoroutineLifecycleTUnitTests.cs:354
-- @test: CoroutineLifecycleTUnitTests.CloseNotStartedCoroutineReturnsTrue
-- Test targets Lua 5.1
function never_started() return 5 end
