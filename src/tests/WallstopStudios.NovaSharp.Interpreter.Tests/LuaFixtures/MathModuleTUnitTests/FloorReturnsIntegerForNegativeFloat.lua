-- @lua-versions: 5.3+
-- @novasharp-only: false
-- @expects-error: false
-- @source: src\tests\WallstopStudios.NovaSharp.Interpreter.Tests.TUnit\Modules\MathModuleTUnitTests.cs:526
-- @test: MathModuleTUnitTests.FloorReturnsIntegerForNegativeFloat
-- @compat-notes: Test targets Lua 5.3+
return math.floor(-3.7)
