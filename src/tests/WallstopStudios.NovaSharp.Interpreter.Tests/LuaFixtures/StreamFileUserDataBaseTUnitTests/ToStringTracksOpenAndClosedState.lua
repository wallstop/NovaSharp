-- @lua-versions: novasharp-only
-- @novasharp-only: true
-- @expects-error: false
-- @source: src/tests/WallstopStudios.NovaSharp.Interpreter.Tests.TUnit/Modules/StreamFileUserDataBaseTUnitTests.cs:1082
-- @test: StreamFileUserDataBaseTUnitTests.ToStringTracksOpenAndClosedState
-- @compat-notes: Uses injected variable: file
file:close()
