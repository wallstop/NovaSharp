-- @lua-versions: 5.5
-- @novasharp-only: false
-- @expects-error: false
-- @source: src/tests/WallstopStudios.NovaSharp.Interpreter.Tests.TUnit/Modules/MathModuleTUnitTests.cs:186
-- @test: MathModuleTUnitTests.PowIsNilInLua55
-- @compat-notes: Test targets Lua 5.5+
return math.pow
