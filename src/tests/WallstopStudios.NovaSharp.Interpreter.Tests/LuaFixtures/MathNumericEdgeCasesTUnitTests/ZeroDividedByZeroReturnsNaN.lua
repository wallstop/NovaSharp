-- @lua-versions: 5.1, 5.2, 5.3, 5.4, 5.5
-- @novasharp-only: false
-- @expects-error: false
-- @source: src/tests/WallstopStudios.NovaSharp.Interpreter.Tests.TUnit/Modules/MathNumericEdgeCasesTUnitTests.cs:266
-- @test: MathNumericEdgeCasesTUnitTests.ZeroDividedByZeroReturnsNaN
-- @compat-notes: Test targets Lua 5.1
return 0 / 0
