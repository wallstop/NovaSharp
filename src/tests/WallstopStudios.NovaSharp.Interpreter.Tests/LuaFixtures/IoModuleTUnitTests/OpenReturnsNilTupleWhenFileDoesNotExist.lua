-- @lua-versions: 5.1+
-- @novasharp-only: false
-- @expects-error: false
-- @source: src/tests/NovaSharp.Interpreter.Tests.TUnit/Modules/IoModuleTUnitTests.cs:32
-- @test: IoModuleTUnitTests.OpenReturnsNilTupleWhenFileDoesNotExist
return io.open('{path}', 'r')
