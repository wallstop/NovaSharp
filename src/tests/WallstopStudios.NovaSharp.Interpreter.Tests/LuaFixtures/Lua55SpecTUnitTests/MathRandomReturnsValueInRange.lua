-- @lua-versions: 5.1+
-- @novasharp-only: false
-- @expects-error: false
-- @source: src/tests/WallstopStudios.NovaSharp.Interpreter.Tests.TUnit/Spec/Lua55SpecTUnitTests.cs:129
-- @test: Lua55SpecTUnitTests.MathRandomReturnsValueInRange
math.randomseed(12345)
