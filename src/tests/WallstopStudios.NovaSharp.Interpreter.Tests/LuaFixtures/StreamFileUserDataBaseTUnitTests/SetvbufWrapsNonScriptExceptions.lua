-- @lua-versions: novasharp-only
-- @novasharp-only: true
-- @expects-error: false
-- @source: src/tests/WallstopStudios.NovaSharp.Interpreter.Tests.TUnit/Modules/StreamFileUserDataBaseTUnitTests.cs:311
-- @test: StreamFileUserDataBaseTUnitTests.SetvbufWrapsNonScriptExceptions
-- Compatibility notes: Uses injected variable: file
file:write('buffer')
