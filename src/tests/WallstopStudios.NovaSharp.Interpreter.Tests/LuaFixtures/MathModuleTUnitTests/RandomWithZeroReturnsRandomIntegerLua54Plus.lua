-- @lua-versions: 5.4, 5.5
-- @novasharp-only: false
-- @expects-error: true
-- @source: src/tests/WallstopStudios.NovaSharp.Interpreter.Tests.TUnit/Modules/MathModuleTUnitTests.cs:1023
-- @test: MathModuleTUnitTests.RandomWithZeroReturnsRandomIntegerLua54Plus
-- @compat-notes: Test targets Lua 5.4+
return math.random(0)
