-- @lua-versions: 5.3+
-- @novasharp-only: false
-- @expects-error: false
-- @source: src\tests\WallstopStudios.NovaSharp.Interpreter.Tests.TUnit\Modules\MathModuleTUnitTests.cs:616
-- @test: MathModuleTUnitTests.CeilReturnsIntegerForPositiveFloat
-- @compat-notes: Test targets Lua 5.3+
return math.ceil(3.2)
