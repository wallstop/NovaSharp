-- @lua-versions: 5.1+
-- @novasharp-only: true
-- @expects-error: false
-- @source: src/tests/NovaSharp.Interpreter.Tests.TUnit/Modules/StreamFileUserDataBaseTUnitTests.cs:772
-- @test: StreamFileUserDataBaseTUnitTests.ReadNumberReturnsNilWhenReaderCannotConsumeChar
return file:read('*n'), file:read('*l')
