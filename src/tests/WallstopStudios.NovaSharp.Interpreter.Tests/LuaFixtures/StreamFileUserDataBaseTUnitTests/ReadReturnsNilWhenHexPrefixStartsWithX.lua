-- @lua-versions: novasharp-only
-- @novasharp-only: true
-- @expects-error: false
-- @source: src/tests/WallstopStudios.NovaSharp.Interpreter.Tests.TUnit/Modules/StreamFileUserDataBaseTUnitTests.cs:646
-- @test: StreamFileUserDataBaseTUnitTests.ReadReturnsNilWhenHexPrefixStartsWithX
-- @compat-notes: Uses injected variable: file
return file:read('*n'), file:read('*a')
