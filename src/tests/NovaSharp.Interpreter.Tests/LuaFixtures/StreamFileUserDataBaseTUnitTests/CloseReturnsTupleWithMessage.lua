-- @lua-versions: 5.1+
-- @novasharp-only: false
-- @expects-error: true
-- @source: src/tests/NovaSharp.Interpreter.Tests.TUnit/Modules/StreamFileUserDataBaseTUnitTests.cs:102
-- @test: StreamFileUserDataBaseTUnitTests.CloseReturnsTupleWithMessage
return file:close()
