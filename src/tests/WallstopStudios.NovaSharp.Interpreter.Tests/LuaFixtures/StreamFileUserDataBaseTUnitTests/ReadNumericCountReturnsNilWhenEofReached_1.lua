-- @lua-versions: novasharp-only
-- @novasharp-only: true
-- @expects-error: true
-- @source: src/tests/WallstopStudios.NovaSharp.Interpreter.Tests.TUnit/Modules/StreamFileUserDataBaseTUnitTests.cs:978
-- @test: StreamFileUserDataBaseTUnitTests.ReadNumericCountReturnsNilWhenEofReached
-- @compat-notes: Uses injected variable: file
return file:read(4)
