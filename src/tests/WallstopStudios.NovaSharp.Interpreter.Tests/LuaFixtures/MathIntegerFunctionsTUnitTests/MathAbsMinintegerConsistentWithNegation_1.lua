-- @lua-versions: 5.3+
-- @novasharp-only: false
-- @expects-error: false
-- @source: src\tests\WallstopStudios.NovaSharp.Interpreter.Tests.TUnit\Modules\MathIntegerFunctionsTUnitTests.cs:144
-- @test: MathIntegerFunctionsTUnitTests.MathAbsMinintegerConsistentWithNegation
-- Lua 5.3+: math.mininteger (5.3+)
return -math.mininteger
