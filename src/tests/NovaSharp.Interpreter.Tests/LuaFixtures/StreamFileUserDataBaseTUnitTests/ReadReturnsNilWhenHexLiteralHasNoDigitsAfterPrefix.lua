-- @lua-versions: 5.1+
-- @novasharp-only: false
-- @expects-error: false
-- @source: src/tests/NovaSharp.Interpreter.Tests.TUnit/Modules/StreamFileUserDataBaseTUnitTests.cs:698
-- @test: StreamFileUserDataBaseTUnitTests.ReadReturnsNilWhenHexLiteralHasNoDigitsAfterPrefix
return file:read('*n'), file:read('*a')
