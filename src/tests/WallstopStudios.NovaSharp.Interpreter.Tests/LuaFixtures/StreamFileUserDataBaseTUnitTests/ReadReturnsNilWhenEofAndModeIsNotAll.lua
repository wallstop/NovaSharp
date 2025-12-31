-- @lua-versions: novasharp-only
-- @novasharp-only: true
-- @expects-error: false
-- @source: src\tests\WallstopStudios.NovaSharp.Interpreter.Tests.TUnit\Modules\StreamFileUserDataBaseTUnitTests.cs:895
-- @test: StreamFileUserDataBaseTUnitTests.ReadReturnsNilWhenEofAndModeIsNotAll
-- @compat-notes: Uses injected variable: file
return file:read('*l')
