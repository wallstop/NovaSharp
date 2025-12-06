-- @lua-versions: 5.1+
-- @novasharp-only: false
-- @expects-error: false
-- @source: src/tests/NovaSharp.Interpreter.Tests.TUnit/Modules/StreamFileUserDataBaseTUnitTests.cs:881
-- @test: StreamFileUserDataBaseTUnitTests.ReadAllReturnsEmptyStringWhenAlreadyAtEof
return file:read('*a')
