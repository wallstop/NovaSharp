-- @lua-versions: 5.4, 5.5
-- @novasharp-only: false
-- @expects-error: false
-- @source: src/tests/WallstopStudios.NovaSharp.Interpreter.Tests.TUnit/Modules/MathNumericEdgeCasesTUnitTests.cs:248
-- @test: MathNumericEdgeCasesTUnitTests.InfinityPlusInfinityIsInfinity
-- @compat-notes: Test targets Lua 5.4+; Lua 5.3+: bitwise operators
local inf = 1/0; return inf + inf
