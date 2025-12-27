-- @lua-versions: 5.4, 5.5
-- @novasharp-only: false
-- @expects-error: false
-- @source: src/tests/WallstopStudios.NovaSharp.Interpreter.Tests.TUnit/Modules/MathModuleTUnitTests.cs:152
-- @test: MathModuleTUnitTests.SqrtOfNegativeNumberReturnsNaN
-- @compat-notes: Test targets Lua 5.4+
return math.sqrt(-1)
