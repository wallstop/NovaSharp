-- @lua-versions: 5.1+
-- @novasharp-only: false
-- @expects-error: false
-- @source: src/tests/NovaSharp.Interpreter.Tests.TUnit/Modules/IoModuleTUnitTests.cs:198
-- @test: IoModuleTUnitTests.NumericIndexOnFileHandleReturnsNil
return io.stdin[1]
