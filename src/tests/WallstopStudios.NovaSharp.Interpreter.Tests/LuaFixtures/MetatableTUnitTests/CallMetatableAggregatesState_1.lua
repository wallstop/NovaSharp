-- @lua-versions: 5.3, 5.4, 5.5
-- @novasharp-only: false
-- @expects-error: false
-- @source: src/tests/WallstopStudios.NovaSharp.Interpreter.Tests.TUnit/Units/Execution/MetatableTUnitTests.cs:79
-- @test: MetatableTUnitTests.CallMetatableAggregatesState
-- Compatibility notes: Test targets Lua 5.3+
return subject(3)
