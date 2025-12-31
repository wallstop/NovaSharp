-- @lua-versions: 5.4, 5.5
-- @novasharp-only: false
-- @expects-error: false
-- @source: src\tests\WallstopStudios.NovaSharp.Interpreter.Tests.TUnit\Modules\MathModuleTUnitTests.cs:1385
-- @test: MathModuleTUnitTests.Log10ReturnsCorrectValues
-- @compat-notes: Test targets Lua 5.4+
return math.log10(10)
