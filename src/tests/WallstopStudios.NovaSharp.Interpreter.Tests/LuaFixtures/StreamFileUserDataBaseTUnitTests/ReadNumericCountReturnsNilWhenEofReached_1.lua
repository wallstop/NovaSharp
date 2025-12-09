-- @lua-versions: 5.1+
-- @novasharp-only: true
-- @expects-error: true
-- @source: src/tests/NovaSharp.Interpreter.Tests.TUnit/Modules/StreamFileUserDataBaseTUnitTests.cs:978
-- @test: StreamFileUserDataBaseTUnitTests.ReadNumericCountReturnsNilWhenEofReached
return file:read(4)
