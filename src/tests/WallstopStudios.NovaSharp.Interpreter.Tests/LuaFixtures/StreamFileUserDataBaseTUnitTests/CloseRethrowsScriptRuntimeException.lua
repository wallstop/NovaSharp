-- @lua-versions: novasharp-only
-- @novasharp-only: true
-- @expects-error: false
-- @source: src\tests\WallstopStudios.NovaSharp.Interpreter.Tests.TUnit\Modules\StreamFileUserDataBaseTUnitTests.cs:136
-- @test: StreamFileUserDataBaseTUnitTests.CloseRethrowsScriptRuntimeException
-- Uses injected variable: file
return file:close()
