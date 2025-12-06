-- @lua-versions: 5.1+
-- @novasharp-only: false
-- @expects-error: false
-- @source: src/tests/NovaSharp.Interpreter.Tests.TUnit/Modules/StreamFileUserDataBaseTUnitTests.cs:1233
-- @test: StreamFileUserDataBaseTUnitTests.BinaryModePreservesCrlfSequence
return file:read('*a')
