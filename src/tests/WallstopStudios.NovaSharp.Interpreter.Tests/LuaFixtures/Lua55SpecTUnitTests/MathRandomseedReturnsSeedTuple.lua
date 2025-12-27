-- @lua-versions: 5.5
-- @novasharp-only: false
-- @expects-error: false
-- @source: src/tests/WallstopStudios.NovaSharp.Interpreter.Tests.TUnit/Spec/Lua55SpecTUnitTests.cs:141
-- @test: Lua55SpecTUnitTests.MathRandomseedReturnsSeedTuple
-- @compat-notes: Test targets Lua 5.5+
return math.randomseed(12345)
