-- @lua-versions: 5.4+
-- @novasharp-only: false
-- @expects-error: true
-- @source: src\tests\WallstopStudios.NovaSharp.Interpreter.Tests.TUnit\Modules\MathModuleTUnitTests.cs:1062
-- @test: MathModuleTUnitTests.RandomWithNegativeUpperBoundThrowsError
-- Test targets Lua 5.4+
return math.random(-5)
