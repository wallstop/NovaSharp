-- @lua-versions: 5.1
-- @novasharp-only: false
-- @expects-error: false
-- @source: src/tests/WallstopStudios.NovaSharp.Interpreter.Tests.TUnit/Modules/MathNumericEdgeCasesTUnitTests.cs:1260
-- @test: MathNumericEdgeCasesTUnitTests.FloorReturnsNumberInLua51And52
-- Compatibility notes: Test targets Lua 5.1
return math.floor(3.7)
