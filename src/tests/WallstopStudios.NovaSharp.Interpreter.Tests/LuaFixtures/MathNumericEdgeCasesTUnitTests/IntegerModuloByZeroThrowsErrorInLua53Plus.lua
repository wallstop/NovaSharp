-- @lua-versions: 5.3, 5.4, 5.5
-- @novasharp-only: false
-- @expects-error: false
-- @source: src/tests/WallstopStudios.NovaSharp.Interpreter.Tests.TUnit/Modules/MathNumericEdgeCasesTUnitTests.cs:308
-- @test: MathNumericEdgeCasesTUnitTests.IntegerModuloByZeroThrowsErrorInLua53Plus
-- @compat-notes: Test targets Lua 5.3+
return 5 % 0
