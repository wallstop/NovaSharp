-- @lua-versions: 5.5
-- @novasharp-only: false
-- @expects-error: false
-- @source: src/tests/WallstopStudios.NovaSharp.Interpreter.Tests.TUnit/Modules/MathNumericEdgeCasesTUnitTests.cs:626
-- @test: MathNumericEdgeCasesTUnitTests.VeryLargeNumberIsFloat
-- @compat-notes: Test targets Lua 5.1; Lua 5.3+: math.type (5.3+)
return math.type(1e100)
