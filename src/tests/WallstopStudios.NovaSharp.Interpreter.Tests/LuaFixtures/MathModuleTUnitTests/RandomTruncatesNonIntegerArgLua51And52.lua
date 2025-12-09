-- @lua-versions: 5.1, 5.5
-- @novasharp-only: false
-- @expects-error: false
-- @source: src/tests/WallstopStudios.NovaSharp.Interpreter.Tests.TUnit/Modules/MathModuleTUnitTests.cs:532
-- @test: MathModuleTUnitTests.RandomTruncatesNonIntegerArgLua51And52
-- @compat-notes: Test targets Lua 5.1
return math.random(1.9)
