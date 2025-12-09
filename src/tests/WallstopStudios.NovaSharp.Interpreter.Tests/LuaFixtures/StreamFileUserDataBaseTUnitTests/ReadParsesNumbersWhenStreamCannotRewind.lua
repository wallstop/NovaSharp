-- @lua-versions: 5.1+
-- @novasharp-only: true
-- @expects-error: false
-- @source: src/tests/NovaSharp.Interpreter.Tests.TUnit/Modules/StreamFileUserDataBaseTUnitTests.cs:520
-- @test: StreamFileUserDataBaseTUnitTests.ReadParsesNumbersWhenStreamCannotRewind
return file:read('*n')
