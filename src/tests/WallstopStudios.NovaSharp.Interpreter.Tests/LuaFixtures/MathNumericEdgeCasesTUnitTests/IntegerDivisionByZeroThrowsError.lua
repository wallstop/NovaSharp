-- @lua-versions: 5.4, 5.5
-- @novasharp-only: false
-- @expects-error: false
-- @source: src\tests\WallstopStudios.NovaSharp.Interpreter.Tests.TUnit\Modules\MathNumericEdgeCasesTUnitTests.cs:202
-- @test: MathNumericEdgeCasesTUnitTests.IntegerDivisionByZeroThrowsError
-- @compat-notes: Test targets Lua 5.4+
return 5 // 0
