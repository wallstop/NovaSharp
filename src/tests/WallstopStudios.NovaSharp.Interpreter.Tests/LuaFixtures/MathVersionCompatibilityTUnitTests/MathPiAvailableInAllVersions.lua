-- @lua-versions: 5.1, 5.5
-- @novasharp-only: false
-- @expects-error: false
-- @source: src/tests/WallstopStudios.NovaSharp.Interpreter.Tests.TUnit/Modules/MathVersionCompatibilityTUnitTests.cs:414
-- @test: MathVersionCompatibilityTUnitTests.MathPiAvailableInAllVersions
-- @compat-notes: Test targets Lua 5.1
return math.pi
