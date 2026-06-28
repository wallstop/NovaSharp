-- @lua-versions: novasharp-only
-- @novasharp-only: true
-- @expects-error: false
-- @source: src\tests\WallstopStudios.NovaSharp.Interpreter.Tests.TUnit\Modules\StreamFileUserDataBaseTUnitTests.cs:698
-- @test: StreamFileUserDataBaseTUnitTests.ReadReturnsNilWhenHexLiteralHasNoDigitsAfterPrefix
-- Uses injected variable: file
return file:read('*n'), file:read('*a')
