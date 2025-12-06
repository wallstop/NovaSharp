-- @lua-versions: 5.1+
-- @novasharp-only: false
-- @expects-error: false
-- @source: src/tests/NovaSharp.Interpreter.Tests.TUnit/Modules/StreamFileUserDataBaseTUnitTests.cs:430
-- @test: StreamFileUserDataBaseTUnitTests.ReadSupportsLineAndBlockModes
return file:read(), file:read('*a')
