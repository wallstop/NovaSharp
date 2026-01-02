-- @lua-versions: 5.3+
-- @novasharp-only: false
-- @expects-error: false
-- @source: src\tests\WallstopStudios.NovaSharp.Interpreter.Tests.TUnit\Modules\MathVersionCompatibilityTUnitTests.cs:247
-- @test: MathVersionCompatibilityTUnitTests.MathIntegerConstantsShouldBeNilInPreLua53
-- @compat-notes: Test targets Lua 5.2+; Lua 5.3+: math.maxinteger (5.3+)
return math.maxinteger
