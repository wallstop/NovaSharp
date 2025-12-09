-- @lua-versions: 5.5
-- @novasharp-only: false
-- @expects-error: false
-- @source: src\tests\WallstopStudios.NovaSharp.Interpreter.Tests.TUnit\Modules\MathNumericEdgeCasesTUnitTests.cs:296
-- @test: MathNumericEdgeCasesTUnitTests.InfinityPlusInfinityIsInfinity
-- @compat-notes: Test targets Lua 5.1; Lua 5.3+: bitwise operators
local inf = 1/0; return inf + inf
