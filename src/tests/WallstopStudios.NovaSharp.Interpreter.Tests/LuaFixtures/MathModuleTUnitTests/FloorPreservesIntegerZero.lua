-- @lua-versions: 5.1+
-- @novasharp-only: false
-- @expects-error: false
-- @source: src\tests\WallstopStudios.NovaSharp.Interpreter.Tests.TUnit\Modules\MathModuleTUnitTests.cs:399
-- @test: MathModuleTUnitTests.FloorPreservesIntegerZero
return math.floor(0)
