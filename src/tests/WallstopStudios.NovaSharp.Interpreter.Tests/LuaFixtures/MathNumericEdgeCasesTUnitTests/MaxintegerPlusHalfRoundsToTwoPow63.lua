-- @lua-versions: 5.3, 5.4, 5.5
-- @novasharp-only: false
-- @expects-error: false
-- @source: src/tests/WallstopStudios.NovaSharp.Interpreter.Tests.TUnit/Modules/MathNumericEdgeCasesTUnitTests.cs:928
-- @test: MathNumericEdgeCasesTUnitTests.MaxintegerPlusHalfRoundsToTwoPow63
-- @compat-notes: Test targets Lua 5.3+; Lua 5.3+: math.type (5.3+); Lua 5.3+: math.maxinteger (5.3+)
local v = math.maxinteger + 0.5
                return v, math.type(v), v == 2^63
