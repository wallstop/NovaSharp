-- @lua-versions: 5.1+
-- @novasharp-only: false
-- @expects-error: false
-- @source: src/tests/WallstopStudios.NovaSharp.Interpreter.Tests.TUnit/Sandbox/DeterministicExecutionTUnitTests.cs:376
-- @test: DeterministicExecutionTUnitTests.OsClockUsesDeterministicTimeProvider
return os.clock()
