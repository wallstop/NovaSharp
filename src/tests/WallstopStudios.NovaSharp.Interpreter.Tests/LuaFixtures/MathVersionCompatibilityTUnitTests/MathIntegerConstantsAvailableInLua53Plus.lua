-- @lua-versions: 5.3+
-- @novasharp-only: false
-- @expects-error: false
-- @source: src\tests\WallstopStudios.NovaSharp.Interpreter.Tests.TUnit\Modules\MathVersionCompatibilityTUnitTests.cs:278
-- @test: MathVersionCompatibilityTUnitTests.MathIntegerConstantsAvailableInLua53Plus
-- @compat-notes: Test targets Lua 5.3+; Lua 5.3+: math.maxinteger (5.3+)
return math.maxinteger
