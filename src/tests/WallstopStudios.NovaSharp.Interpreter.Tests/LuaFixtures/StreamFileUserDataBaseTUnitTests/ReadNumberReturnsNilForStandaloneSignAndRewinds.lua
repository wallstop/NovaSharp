-- @lua-versions: 5.1+
-- @novasharp-only: true
-- @expects-error: false
-- @source: src/tests/NovaSharp.Interpreter.Tests.TUnit/Modules/StreamFileUserDataBaseTUnitTests.cs:962
-- @test: StreamFileUserDataBaseTUnitTests.ReadNumberReturnsNilForStandaloneSignAndRewinds
return file:read('*n', '*l')
