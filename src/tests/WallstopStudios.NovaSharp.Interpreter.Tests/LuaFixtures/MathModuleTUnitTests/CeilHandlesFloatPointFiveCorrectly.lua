-- @lua-versions: 5.3, 5.4, 5.5
-- @novasharp-only: false
-- @expects-error: false
-- @source: src/tests/WallstopStudios.NovaSharp.Interpreter.Tests.TUnit/Modules/MathModuleTUnitTests.cs:701
-- @test: MathModuleTUnitTests.CeilHandlesFloatPointFiveCorrectly
-- @compat-notes: Test targets Lua 5.3+
return math.ceil(0.5)
