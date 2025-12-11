-- @lua-versions: 5.1, 5.2, 5.3, 5.4
-- @novasharp-only: false
-- @expects-error: false
-- @source: src\tests\WallstopStudios.NovaSharp.Interpreter.Tests.TUnit\Modules\MathModuleTUnitTests.cs:104
-- @test: MathModuleTUnitTests.PowWithLargeExponentReturnsInfinity
-- Note: math.pow was deprecated in Lua 5.3 and removed in Lua 5.5. Use x^y instead.
return math.pow(10, 309)
