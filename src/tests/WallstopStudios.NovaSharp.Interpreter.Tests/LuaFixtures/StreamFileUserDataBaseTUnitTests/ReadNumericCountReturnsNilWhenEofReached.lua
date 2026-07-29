-- @lua-versions: novasharp-only
-- @novasharp-only: true
-- @expects-error: false
-- @source: src\tests\WallstopStudios.NovaSharp.Interpreter.Tests.TUnit\Modules\StreamFileUserDataBaseTUnitTests.cs:976
-- @test: StreamFileUserDataBaseTUnitTests.ReadNumericCountReturnsNilWhenEofReached
-- Uses injected variable: file
file:read('*a')
