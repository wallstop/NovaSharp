-- @lua-versions: 5.1+
-- @novasharp-only: true
-- @expects-error: false
-- @source: src/tests/NovaSharp.Interpreter.Tests.TUnit/Modules/StreamFileUserDataBaseTUnitTests.cs:646
-- @test: StreamFileUserDataBaseTUnitTests.ReadReturnsNilWhenHexPrefixStartsWithX
return file:read('*n'), file:read('*a')
