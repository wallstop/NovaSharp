-- @lua-versions: 5.1+
-- @novasharp-only: false
-- @expects-error: false
-- @source: src/tests/WallstopStudios.NovaSharp.Interpreter.Tests.TUnit/Modules/MathModuleTUnitTests.cs:353
-- @test: MathModuleTUnitTests.FloorHandlesInfinityAsFloat
return math.floor(1/0)
