-- @lua-versions: 5.1+
-- @novasharp-only: true
-- @expects-error: false
-- @source: src/tests/NovaSharp.Interpreter.Tests.TUnit/Modules/StreamFileUserDataBaseTUnitTests.cs:136
-- @test: StreamFileUserDataBaseTUnitTests.CloseRethrowsScriptRuntimeException
return file:close()
