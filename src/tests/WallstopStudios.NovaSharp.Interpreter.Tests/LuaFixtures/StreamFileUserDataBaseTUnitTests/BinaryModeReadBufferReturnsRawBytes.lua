-- @lua-versions: novasharp-only
-- @novasharp-only: true
-- @expects-error: false
-- @source: src/tests/WallstopStudios.NovaSharp.Interpreter.Tests.TUnit/Modules/StreamFileUserDataBaseTUnitTests.cs:1247
-- @test: StreamFileUserDataBaseTUnitTests.BinaryModeReadBufferReturnsRawBytes
-- @compat-notes: Uses injected variable: file
return file:read(4), file:read('*a')
