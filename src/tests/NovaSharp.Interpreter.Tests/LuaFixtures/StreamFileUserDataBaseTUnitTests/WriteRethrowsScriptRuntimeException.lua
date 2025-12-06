-- @lua-versions: 5.1+
-- @novasharp-only: false
-- @expects-error: true
-- @source: src/tests/NovaSharp.Interpreter.Tests.TUnit/Modules/StreamFileUserDataBaseTUnitTests.cs:87
-- @test: StreamFileUserDataBaseTUnitTests.WriteRethrowsScriptRuntimeException
return file:write('boom')
