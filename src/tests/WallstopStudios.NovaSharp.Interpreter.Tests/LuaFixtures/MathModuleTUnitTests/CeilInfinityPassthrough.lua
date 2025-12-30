-- @lua-versions: 5.1+
-- @novasharp-only: false
-- @expects-error: false
-- @source: src/tests/WallstopStudios.NovaSharp.Interpreter.Tests.TUnit/Modules/MathModuleTUnitTests.cs:2478
-- @test: MathModuleTUnitTests.CeilInfinityPassthrough
return math.ceil(math.huge)
