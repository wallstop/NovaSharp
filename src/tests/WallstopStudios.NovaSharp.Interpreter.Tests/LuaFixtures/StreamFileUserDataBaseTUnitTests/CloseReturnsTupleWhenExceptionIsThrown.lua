-- @lua-versions: 5.1+
-- @novasharp-only: true
-- @expects-error: false
-- @source: src/tests/NovaSharp.Interpreter.Tests.TUnit/Modules/StreamFileUserDataBaseTUnitTests.cs:119
-- @test: StreamFileUserDataBaseTUnitTests.CloseReturnsTupleWhenExceptionIsThrown
return file:close()
