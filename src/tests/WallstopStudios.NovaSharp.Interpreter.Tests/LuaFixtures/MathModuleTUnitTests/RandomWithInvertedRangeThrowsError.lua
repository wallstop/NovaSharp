-- @lua-versions: 5.1+
-- @novasharp-only: false
-- @expects-error: true
-- @source: src/tests/WallstopStudios.NovaSharp.Interpreter.Tests.TUnit/Modules/MathModuleTUnitTests.cs:978
-- @test: MathModuleTUnitTests.RandomWithInvertedRangeThrowsError
return math.random(10, 5)
