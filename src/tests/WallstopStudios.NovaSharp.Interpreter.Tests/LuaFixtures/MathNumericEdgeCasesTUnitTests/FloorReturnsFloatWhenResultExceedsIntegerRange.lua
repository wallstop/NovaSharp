-- @lua-versions: 5.3, 5.4, 5.5
-- @novasharp-only: false
-- @expects-error: false
-- @source: src/tests/WallstopStudios.NovaSharp.Interpreter.Tests.TUnit/Modules/MathNumericEdgeCasesTUnitTests.cs:1176
-- @test: MathNumericEdgeCasesTUnitTests.FloorReturnsFloatWhenResultExceedsIntegerRange
-- @compat-notes: Test targets Lua 5.3+; Lua 5.3+: math.type (5.3+); Lua 5.3+: math.maxinteger (5.3+)
local v = math.floor(math.maxinteger + 0.5)
                return math.type(v), v
