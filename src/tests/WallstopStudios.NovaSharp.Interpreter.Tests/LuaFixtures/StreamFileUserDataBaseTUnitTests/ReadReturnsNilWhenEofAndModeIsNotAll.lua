-- @lua-versions: 5.1+
-- @novasharp-only: true
-- @expects-error: false
-- @source: src/tests/NovaSharp.Interpreter.Tests.TUnit/Modules/StreamFileUserDataBaseTUnitTests.cs:895
-- @test: StreamFileUserDataBaseTUnitTests.ReadReturnsNilWhenEofAndModeIsNotAll
return file:read('*l')
