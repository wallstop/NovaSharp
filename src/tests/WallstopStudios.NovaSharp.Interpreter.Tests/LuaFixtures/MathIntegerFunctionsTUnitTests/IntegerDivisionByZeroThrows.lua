-- @lua-versions: 5.1+
-- @novasharp-only: false
-- @expects-error: false
-- @source: src\tests\WallstopStudios.NovaSharp.Interpreter.Tests.TUnit\Modules\MathIntegerFunctionsTUnitTests.cs:450
-- @test: MathIntegerFunctionsTUnitTests.IntegerDivisionByZeroThrows
return 1 // 0
