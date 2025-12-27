-- @lua-versions: 5.1
-- @novasharp-only: false
-- @expects-error: false
-- @source: src/tests/WallstopStudios.NovaSharp.Interpreter.Tests.TUnit/Modules/MathModuleTUnitTests.cs:1380
-- @test: MathModuleTUnitTests.ModAvailableOnlyInLua51
-- @compat-notes: Test targets Lua 5.1; Lua 5.1 only: math.mod (5.1 only, use math.fmod)
return math.mod(10, 3)
