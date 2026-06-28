-- @lua-versions: 5.1+
-- @novasharp-only: false
-- @expects-error: false
-- @source: src\tests\WallstopStudios.NovaSharp.Interpreter.Tests.TUnit\Modules\MathModuleTUnitTests.cs:950
-- @test: MathModuleTUnitTests.RandomErrorsOnTooManyArguments
return math.random(1, 2, 3)
