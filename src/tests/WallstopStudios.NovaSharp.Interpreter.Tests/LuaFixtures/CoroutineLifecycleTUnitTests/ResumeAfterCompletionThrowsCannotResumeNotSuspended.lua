-- @lua-versions: 5.1
-- @novasharp-only: false
-- @expects-error: false
-- @source: src/tests/WallstopStudios.NovaSharp.Interpreter.Tests.TUnit/Units/DataTypes/CoroutineLifecycleTUnitTests.cs:23
-- @test: CoroutineLifecycleTUnitTests.ResumeAfterCompletionThrowsCannotResumeNotSuspended
-- Test targets Lua 5.1
function simple() return 5 end
