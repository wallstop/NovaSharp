-- @lua-versions: 5.3+
-- @novasharp-only: false
-- @expects-error: false
-- @source: src\tests\WallstopStudios.NovaSharp.Interpreter.Tests.TUnit\Modules\MathModuleTUnitTests.cs:359
-- @test: MathModuleTUnitTests.UltPerformsUnsignedComparison
-- Test targets Lua 5.3+; Lua 5.3+: math.ult (5.3+)
return math.ult(0, -1), math.ult(-1, 0)
