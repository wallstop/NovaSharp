-- @lua-versions: 5.5
-- @novasharp-only: false
-- @expects-error: false
-- @source: src/tests/WallstopStudios.NovaSharp.Interpreter.Tests.TUnit/Modules/MathModuleTUnitTests.cs:1381
-- @test: MathModuleTUnitTests.Log10IsNilInLua55
-- @compat-notes: Test targets Lua 5.5+
return math.log10
