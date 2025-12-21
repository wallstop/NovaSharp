-- @lua-versions: 5.3, 5.4, 5.5
-- @novasharp-only: false
-- @expects-error: false
-- @source: src/tests/WallstopStudios.NovaSharp.Interpreter.Tests.TUnit/Modules/MathModuleTUnitTests.cs:1353
-- @test: MathModuleTUnitTests.ModfReturnsIntegerSubtypeInLua53Plus
-- @compat-notes: Test targets Lua 5.3+; Lua 5.3+: math.type (5.3+)
local i, f = math.modf(3.5); return math.type(i), i, math.type(f), f
