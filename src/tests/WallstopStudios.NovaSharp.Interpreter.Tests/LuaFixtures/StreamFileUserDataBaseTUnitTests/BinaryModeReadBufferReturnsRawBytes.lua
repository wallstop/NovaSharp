-- @lua-versions: 5.1+
-- @novasharp-only: true
-- @expects-error: false
-- @source: src/tests/NovaSharp.Interpreter.Tests.TUnit/Modules/StreamFileUserDataBaseTUnitTests.cs:1247
-- @test: StreamFileUserDataBaseTUnitTests.BinaryModeReadBufferReturnsRawBytes
return file:read(4), file:read('*a')
