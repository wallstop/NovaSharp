-- @lua-versions: 5.1+
-- @novasharp-only: false
-- @expects-error: false
-- @source: src/tests/WallstopStudios.NovaSharp.Interpreter.Tests.TUnit/Modules/MathModuleTUnitTests.cs:18
-- @test: MathModuleTUnitTests.LogUsesDefaultBaseWhenOmitted
return math.log(8)
