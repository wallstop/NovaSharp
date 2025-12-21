-- @lua-versions: 5.1, 5.5
-- @novasharp-only: false
-- @expects-error: false
-- @source: src/tests/WallstopStudios.NovaSharp.Interpreter.Tests.TUnit/Modules/MathModuleTUnitTests.cs:1331
-- @test: MathModuleTUnitTests.ModIsNilInLua52Plus
-- @compat-notes: Test targets Lua 5.1
return math.mod
