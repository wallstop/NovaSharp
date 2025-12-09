-- @lua-versions: 5.1+
-- @novasharp-only: false
-- @expects-error: false
-- @source: src/tests/WallstopStudios.NovaSharp.Interpreter.Tests.TUnit/Modules/MathModuleTUnitTests.cs:440
-- @test: MathModuleTUnitTests.CeilHandlesInfinityAsFloat
return math.ceil(1/0)
