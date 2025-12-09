-- @lua-versions: 5.1+
-- @novasharp-only: false
-- @expects-error: false
-- @source: src/tests/WallstopStudios.NovaSharp.Interpreter.Tests.TUnit/Modules/MathModuleTUnitTests.cs:401
-- @test: MathModuleTUnitTests.CeilReturnsIntegerForPositiveFloat
return math.ceil(3.2)
