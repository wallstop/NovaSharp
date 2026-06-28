-- @lua-versions: novasharp-only
-- @novasharp-only: true
-- @expects-error: false
-- @source: src\tests\WallstopStudios.NovaSharp.Interpreter.Tests.TUnit\Modules\MathModuleTUnitTests.cs:2570
-- @test: MathModuleTUnitTests.MathTypeDistinguishesNumericTypes
-- NovaSharp: unresolved C# interpolation placeholder; Test targets Lua 5.3+
return math.type({expression})
