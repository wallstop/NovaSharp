-- @lua-versions: novasharp-only
-- @novasharp-only: true
-- @expects-error: false
-- @source: src/tests/WallstopStudios.NovaSharp.Interpreter.Tests.TUnit/Modules/StreamFileUserDataBaseTUnitTests.cs:520
-- @test: StreamFileUserDataBaseTUnitTests.ReadParsesNumbersWhenStreamCannotRewind
-- Compatibility notes: Uses injected variable: file
return file:read('*n')
