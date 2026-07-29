-- @lua-versions: 5.1+
-- @novasharp-only: false
-- @expects-error: false
-- @source: src/tests/WallstopStudios.NovaSharp.Interpreter.Tests.TUnit/Units/DataTypes/CoroutineLifecycleTUnitTests.cs:354
-- @test: CoroutineLifecycleTUnitTests.CloseNotStartedCoroutineReturnsTrue
function never_started() return 5 end
