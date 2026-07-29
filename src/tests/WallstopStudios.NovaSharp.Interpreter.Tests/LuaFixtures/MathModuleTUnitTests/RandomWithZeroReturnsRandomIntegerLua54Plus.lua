-- @lua-versions: 5.4+
-- @novasharp-only: false
-- @expects-error: true
-- @source: src\tests\WallstopStudios.NovaSharp.Interpreter.Tests.TUnit\Modules\MathModuleTUnitTests.cs:1047
-- @test: MathModuleTUnitTests.RandomWithZeroReturnsRandomIntegerLua54Plus
-- Test targets Lua 5.4+
return math.random(0)
