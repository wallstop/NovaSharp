-- @lua-versions: novasharp-only
-- @novasharp-only: true
-- @expects-error: true
-- @source: src/tests/WallstopStudios.NovaSharp.Interpreter.Tests.TUnit/Modules/StreamFileUserDataBaseTUnitTests.cs:87
-- @test: StreamFileUserDataBaseTUnitTests.WriteRethrowsScriptRuntimeException
-- @compat-notes: Uses injected variable: file
return file:write('boom')
