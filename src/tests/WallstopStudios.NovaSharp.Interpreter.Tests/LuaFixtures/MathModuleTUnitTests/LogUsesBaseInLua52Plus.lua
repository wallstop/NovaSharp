-- @lua-versions: 5.2+
-- @novasharp-only: false
-- @expects-error: false
-- @source: src\tests\WallstopStudios.NovaSharp.Interpreter.Tests.TUnit\Modules\MathModuleTUnitTests.cs:1337
-- @test: MathModuleTUnitTests.LogUsesBaseInLua52Plus
-- Test targets Lua 5.2+
return math.log(100, 10)
