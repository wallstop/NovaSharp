-- @lua-versions: novasharp-only
-- @novasharp-only: true
-- @expects-error: false
-- @source: src\tests\WallstopStudios.NovaSharp.Interpreter.Tests.TUnit\Modules\StreamFileUserDataBaseTUnitTests.cs:962
-- @test: StreamFileUserDataBaseTUnitTests.ReadNumberReturnsNilForStandaloneSignAndRewinds
-- @compat-notes: Uses injected variable: file
return file:read('*n', '*l')
