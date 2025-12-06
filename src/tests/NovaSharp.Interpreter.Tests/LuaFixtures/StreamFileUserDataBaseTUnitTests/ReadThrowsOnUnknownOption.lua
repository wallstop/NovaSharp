-- @lua-versions: 5.1+
-- @novasharp-only: false
-- @expects-error: true
-- @source: src/tests/NovaSharp.Interpreter.Tests.TUnit/Modules/StreamFileUserDataBaseTUnitTests.cs:994
-- @test: StreamFileUserDataBaseTUnitTests.ReadThrowsOnUnknownOption
file:read('*z')
