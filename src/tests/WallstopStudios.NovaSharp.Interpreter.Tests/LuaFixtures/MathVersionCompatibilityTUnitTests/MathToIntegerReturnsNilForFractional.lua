-- @lua-versions: novasharp-only
-- @novasharp-only: true
-- @expects-error: false
-- @source: src\tests\WallstopStudios.NovaSharp.Interpreter.Tests.TUnit\Modules\MathVersionCompatibilityTUnitTests.cs:173
-- @test: MathVersionCompatibilityTUnitTests.MathToIntegerReturnsNilForFractional
-- NovaSharp: unresolved C# interpolation placeholder; Test targets Lua 5.2+
return math.tointeger({input})
