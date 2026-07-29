-- @lua-versions: novasharp-only
-- @novasharp-only: true
-- @expects-error: false
-- @source: src/tests/WallstopStudios.NovaSharp.Interpreter.Tests.TUnit/Modules/StreamFileUserDataBaseTUnitTests.cs:881
-- @test: StreamFileUserDataBaseTUnitTests.ReadAllReturnsEmptyStringWhenAlreadyAtEof
-- Compatibility notes: Uses injected variable: file
return file:read('*a')
