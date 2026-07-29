-- @lua-versions: novasharp-only
-- @novasharp-only: true
-- @expects-error: true
-- @source: src\tests\WallstopStudios.NovaSharp.Interpreter.Tests.TUnit\Modules\StreamFileUserDataBaseTUnitTests.cs:994
-- @test: StreamFileUserDataBaseTUnitTests.ReadThrowsOnUnknownOption
-- Uses injected variable: file
file:read('*z')
