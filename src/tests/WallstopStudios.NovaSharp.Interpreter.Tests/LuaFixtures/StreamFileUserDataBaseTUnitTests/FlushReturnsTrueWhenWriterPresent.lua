-- @lua-versions: novasharp-only
-- @novasharp-only: true
-- @expects-error: false
-- @source: src/tests/WallstopStudios.NovaSharp.Interpreter.Tests.TUnit/Modules/StreamFileUserDataBaseTUnitTests.cs:150
-- @test: StreamFileUserDataBaseTUnitTests.FlushReturnsTrueWhenWriterPresent
-- Compatibility notes: Uses injected variable: file
return file:flush()
