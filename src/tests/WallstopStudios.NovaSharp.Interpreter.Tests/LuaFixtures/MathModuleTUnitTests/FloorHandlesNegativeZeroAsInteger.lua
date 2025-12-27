-- @lua-versions: 5.3, 5.4, 5.5
-- @novasharp-only: false
-- @expects-error: false
-- @source: src/tests/WallstopStudios.NovaSharp.Interpreter.Tests.TUnit/Modules/MathModuleTUnitTests.cs:598
-- @test: MathModuleTUnitTests.FloorHandlesNegativeZeroAsInteger
-- @compat-notes: Test targets Lua 5.3+
return math.floor(-0.0)
