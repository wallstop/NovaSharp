-- @lua-versions: 5.3+
-- @novasharp-only: false
-- @expects-error: false
-- @source: src\tests\WallstopStudios.NovaSharp.Interpreter.Tests.TUnit\Modules\MathModuleTUnitTests.cs:688
-- @test: MathModuleTUnitTests.CeilPreservesIntegerZero
-- Test targets Lua 5.3+
return math.ceil(0)
