-- @lua-versions: 5.1, 5.2, 5.3, 5.4
-- @novasharp-only: false
-- @expects-error: false
-- @source: src/tests/WallstopStudios.NovaSharp.Interpreter.Tests.TUnit/Modules/MathModuleTUnitTests.cs:52
-- @test: MathModuleTUnitTests.PowHandlesLargeExponent
-- @compat-notes: Test targets Lua 5.1
return math.pow(10, 6)
