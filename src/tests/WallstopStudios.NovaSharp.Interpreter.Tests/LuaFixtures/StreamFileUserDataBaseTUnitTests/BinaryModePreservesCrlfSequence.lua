-- @lua-versions: novasharp-only
-- @novasharp-only: true
-- @expects-error: false
-- @source: src/tests/WallstopStudios.NovaSharp.Interpreter.Tests.TUnit/Modules/StreamFileUserDataBaseTUnitTests.cs:1233
-- @test: StreamFileUserDataBaseTUnitTests.BinaryModePreservesCrlfSequence
-- @compat-notes: Uses injected variable: file
return file:read('*a')
