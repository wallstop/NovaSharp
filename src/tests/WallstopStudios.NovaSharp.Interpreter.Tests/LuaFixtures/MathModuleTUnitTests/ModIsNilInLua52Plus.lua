-- @lua-versions: 5.2+
-- @novasharp-only: false
-- @expects-error: false
-- @source: src\tests\WallstopStudios.NovaSharp.Interpreter.Tests.TUnit\Modules\MathModuleTUnitTests.cs:1447
-- @test: MathModuleTUnitTests.ModIsNilInLua52Plus
-- Test targets Lua 5.2+
return math.mod
