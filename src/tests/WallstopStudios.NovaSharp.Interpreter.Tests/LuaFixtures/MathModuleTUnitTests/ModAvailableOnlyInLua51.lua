-- @lua-versions: 5.1, 5.2, 5.3, 5.4, 5.5
-- @novasharp-only: false
-- @expects-error: false
-- @source: src/tests/WallstopStudios.NovaSharp.Interpreter.Tests.TUnit/Modules/MathModuleTUnitTests.cs:1313
-- @test: MathModuleTUnitTests.ModAvailableOnlyInLua51
-- @compat-notes: Test targets Lua 5.1
return math.mod(10, 3)
