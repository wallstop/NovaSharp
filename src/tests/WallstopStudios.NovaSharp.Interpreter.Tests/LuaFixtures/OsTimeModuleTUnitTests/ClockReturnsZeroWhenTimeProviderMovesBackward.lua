-- @lua-versions: 5.1+
-- @novasharp-only: false
-- @expects-error: false
-- @source: src/tests/NovaSharp.Interpreter.Tests.TUnit/Modules/OsTimeModuleTUnitTests.cs:129
-- @test: OsTimeModuleTUnitTests.ClockReturnsZeroWhenTimeProviderMovesBackward
return os.clock()
