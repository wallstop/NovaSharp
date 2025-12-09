-- @lua-versions: 5.1, 5.5
-- @novasharp-only: false
-- @expects-error: false
-- @source: src\tests\WallstopStudios.NovaSharp.Interpreter.Tests.TUnit\Modules\MathNumericEdgeCasesTUnitTests.cs:224
-- @test: MathNumericEdgeCasesTUnitTests.IntegerModuloByZeroThrowsError
-- @compat-notes: Test targets Lua 5.1
return 5 % 0
