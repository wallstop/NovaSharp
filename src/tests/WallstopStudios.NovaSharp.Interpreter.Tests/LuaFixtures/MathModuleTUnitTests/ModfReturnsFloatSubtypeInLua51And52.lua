-- @lua-versions: 5.1, 5.2
-- @novasharp-only: false
-- @expects-error: false
-- @source: src/tests/WallstopStudios.NovaSharp.Interpreter.Tests.TUnit/Modules/MathModuleTUnitTests.cs:1389
-- @test: MathModuleTUnitTests.ModfReturnsFloatSubtypeInLua51And52
-- @compat-notes: Test targets Lua 5.1
local i, f = math.modf(3.5); return type(i), i, type(f), f
