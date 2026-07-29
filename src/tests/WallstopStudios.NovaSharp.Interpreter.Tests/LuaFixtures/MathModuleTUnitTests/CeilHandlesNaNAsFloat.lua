-- @lua-versions: 5.3+
-- @novasharp-only: false
-- @expects-error: false
-- @source: src\tests\WallstopStudios.NovaSharp.Interpreter.Tests.TUnit\Modules\MathModuleTUnitTests.cs:677
-- @test: MathModuleTUnitTests.CeilHandlesNaNAsFloat
-- Test targets Lua 5.3+
return math.ceil(0/0)
