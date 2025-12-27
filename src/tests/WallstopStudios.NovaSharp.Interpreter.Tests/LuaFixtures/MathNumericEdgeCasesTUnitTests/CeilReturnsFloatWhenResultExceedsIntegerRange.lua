-- @lua-versions: 5.3, 5.4, 5.5
-- @novasharp-only: false
-- @expects-error: false
-- @source: src/tests/WallstopStudios.NovaSharp.Interpreter.Tests.TUnit/Modules/MathNumericEdgeCasesTUnitTests.cs:1093
-- @test: MathNumericEdgeCasesTUnitTests.CeilReturnsFloatWhenResultExceedsIntegerRange
-- @compat-notes: Test targets Lua 5.3+; Lua 5.3+: math.type (5.3+); Lua 5.3+: math.mininteger (5.3+)
local v = math.ceil(math.mininteger - 10000000000.0)
                return math.type(v), v
