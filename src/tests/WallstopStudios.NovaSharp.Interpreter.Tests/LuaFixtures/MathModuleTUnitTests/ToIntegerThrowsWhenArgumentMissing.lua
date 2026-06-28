-- @lua-versions: 5.3+
-- @novasharp-only: false
-- @expects-error: true
-- @source: src\tests\WallstopStudios.NovaSharp.Interpreter.Tests.TUnit\Modules\MathModuleTUnitTests.cs:301
-- @test: MathModuleTUnitTests.ToIntegerThrowsWhenArgumentMissing
-- Test targets Lua 5.3+; Lua 5.3+: math.tointeger (5.3+)
return math.tointeger()
