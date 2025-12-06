-- @lua-versions: 5.1+
-- @novasharp-only: false
-- @expects-error: false
-- @source: src/tests/NovaSharp.Interpreter.Tests.TUnit/Modules/StreamFileUserDataBaseTUnitTests.cs:311
-- @test: StreamFileUserDataBaseTUnitTests.SetvbufWrapsNonScriptExceptions
file:write('buffer')
