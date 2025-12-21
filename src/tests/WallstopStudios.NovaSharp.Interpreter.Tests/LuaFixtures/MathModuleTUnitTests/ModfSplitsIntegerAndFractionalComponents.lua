-- @lua-versions: 5.1, 5.5
-- @novasharp-only: false
-- @expects-error: false
-- @source: src/tests/WallstopStudios.NovaSharp.Interpreter.Tests.TUnit/Modules/MathModuleTUnitTests.cs:69
-- @test: MathModuleTUnitTests.ModfSplitsIntegerAndFractionalComponents
-- @compat-notes: Test targets Lua 5.1
return math.modf(-3.25)
