-- @lua-versions: novasharp-only
-- @novasharp-only: true
-- @expects-error: false
-- @source: src\tests\WallstopStudios.NovaSharp.Interpreter.Tests.TUnit\Modules\StreamFileUserDataBaseTUnitTests.cs:430
-- @test: StreamFileUserDataBaseTUnitTests.ReadSupportsLineAndBlockModes
-- Uses injected variable: file
return file:read(), file:read('*a')
