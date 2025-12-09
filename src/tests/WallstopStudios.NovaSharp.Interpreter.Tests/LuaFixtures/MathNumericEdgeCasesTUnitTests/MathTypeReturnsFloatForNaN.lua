-- @lua-versions: 5.4, 5.5
-- @novasharp-only: false
-- @expects-error: false
-- @source: src/tests/WallstopStudios.NovaSharp.Interpreter.Tests.TUnit/Modules/MathNumericEdgeCasesTUnitTests.cs:356
-- @test: MathNumericEdgeCasesTUnitTests.MathTypeReturnsFloatForNaN
-- @compat-notes: Test targets Lua 5.4+; Lua 5.3+: math.type (5.3+)
return math.type(0/0)
