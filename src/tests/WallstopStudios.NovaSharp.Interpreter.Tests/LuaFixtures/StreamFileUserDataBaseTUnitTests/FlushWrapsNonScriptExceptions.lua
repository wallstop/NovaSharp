-- @lua-versions: novasharp-only
-- @novasharp-only: true
-- @expects-error: true
-- @source: src\tests\WallstopStudios.NovaSharp.Interpreter.Tests.TUnit\Modules\StreamFileUserDataBaseTUnitTests.cs:181
-- @test: StreamFileUserDataBaseTUnitTests.FlushWrapsNonScriptExceptions
-- @compat-notes: Uses injected variable: file
return file:flush()
