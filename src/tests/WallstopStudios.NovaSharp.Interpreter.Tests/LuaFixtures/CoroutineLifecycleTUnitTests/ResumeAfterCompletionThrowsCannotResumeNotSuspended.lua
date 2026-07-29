-- @lua-versions: 5.1+
-- @novasharp-only: false
-- @expects-error: false
-- @source: src/tests/WallstopStudios.NovaSharp.Interpreter.Tests.TUnit/Units/DataTypes/CoroutineLifecycleTUnitTests.cs:23
-- @test: CoroutineLifecycleTUnitTests.ResumeAfterCompletionThrowsCannotResumeNotSuspended
function simple() return 5 end
