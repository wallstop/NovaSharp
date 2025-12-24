-- @lua-versions: 5.3, 5.4, 5.5
-- @novasharp-only: false
-- @expects-error: false
-- @source: src/tests/WallstopStudios.NovaSharp.Interpreter.Tests.TUnit/Modules/MathNumericEdgeCasesTUnitTests.cs:352
-- @test: MathNumericEdgeCasesTUnitTests.IntegerModuloByZeroThrowsErrorInLua53Plus
-- @compat-notes: Test targets Lua 5.1
return 5 % 0
