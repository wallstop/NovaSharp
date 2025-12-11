-- @lua-versions: 5.1+
-- @novasharp-only: false
-- @expects-error: false
-- @source: src\tests\WallstopStudios.NovaSharp.Interpreter.Tests.TUnit\Modules\MathModuleTUnitTests.cs:389
-- @test: MathModuleTUnitTests.FloorHandlesNaNAsFloat
return math.floor(0/0)
