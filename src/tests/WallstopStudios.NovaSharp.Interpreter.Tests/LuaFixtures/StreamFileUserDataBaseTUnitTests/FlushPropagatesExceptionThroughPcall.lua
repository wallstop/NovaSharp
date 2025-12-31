-- @lua-versions: novasharp-only
-- @novasharp-only: true
-- @expects-error: false
-- @source: src\tests\WallstopStudios.NovaSharp.Interpreter.Tests.TUnit\Modules\StreamFileUserDataBaseTUnitTests.cs:165
-- @test: StreamFileUserDataBaseTUnitTests.FlushPropagatesExceptionThroughPcall
-- @compat-notes: Uses injected variable: file
return file:flush()
