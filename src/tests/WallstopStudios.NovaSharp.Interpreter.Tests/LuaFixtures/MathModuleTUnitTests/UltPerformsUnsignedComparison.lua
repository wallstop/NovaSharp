-- @lua-versions: 5.3, 5.4, 5.5
-- @novasharp-only: false
-- @expects-error: true
-- @source: src/tests/WallstopStudios.NovaSharp.Interpreter.Tests.TUnit/Modules/MathModuleTUnitTests.cs:203
-- @test: MathModuleTUnitTests.UltPerformsUnsignedComparison
-- @compat-notes: Lua 5.3+: math.ult (5.3+)
return math.ult(0, -1), math.ult(-1, 0)
