-- @lua-versions: 5.4, 5.5
-- @novasharp-only: false
-- @expects-error: false
-- @source: src/tests/WallstopStudios.NovaSharp.Interpreter.Tests.TUnit/Modules/MathNumericEdgeCasesTUnitTests.cs:424
-- @test: MathNumericEdgeCasesTUnitTests.IntegerLeftShiftBy64ReturnsZero
-- @compat-notes: Test targets Lua 5.4+; Lua 5.3+: bit shift
return 1 << 64
