-- @lua-versions: 5.1+
-- @novasharp-only: false
-- @expects-error: true
-- @source: src/tests/NovaSharp.Interpreter.Tests.TUnit/Modules/IoModuleTUnitTests.cs:498
-- @test: IoModuleTUnitTests.OpenReturnsErrorWhenEncodingSpecifiedForBinaryMode
return io.open('{path}', 'rb', 'utf-8')
