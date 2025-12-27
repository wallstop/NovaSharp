-- @lua-versions: 5.5
-- @novasharp-only: false
-- @expects-error: false
-- @source: src/tests/WallstopStudios.NovaSharp.Interpreter.Tests.TUnit/Spec/Lua55SpecTUnitTests.cs:129
-- @test: Lua55SpecTUnitTests.MathRandomReturnsValueInRange
-- @compat-notes: Test targets Lua 5.5+
math.randomseed(12345)
