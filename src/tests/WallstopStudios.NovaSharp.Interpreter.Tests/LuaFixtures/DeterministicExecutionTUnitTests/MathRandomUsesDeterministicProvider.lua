-- @lua-versions: 5.1
-- @novasharp-only: false
-- @expects-error: false
-- @source: src/tests/WallstopStudios.NovaSharp.Interpreter.Tests.TUnit/Sandbox/DeterministicExecutionTUnitTests.cs:302
-- @test: DeterministicExecutionTUnitTests.MathRandomUsesDeterministicProvider
-- @compat-notes: Test targets Lua 5.1
return math.random()
