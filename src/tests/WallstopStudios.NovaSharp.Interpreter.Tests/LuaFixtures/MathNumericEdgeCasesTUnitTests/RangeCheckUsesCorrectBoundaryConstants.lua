-- @lua-versions: 5.3, 5.4, 5.5
-- @novasharp-only: false
-- @expects-error: false
-- @source: src/tests/WallstopStudios.NovaSharp.Interpreter.Tests.TUnit/Modules/MathNumericEdgeCasesTUnitTests.cs:1128
-- @test: MathNumericEdgeCasesTUnitTests.RangeCheckUsesCorrectBoundaryConstants
-- @compat-notes: Test targets Lua 5.3+
local just_under = 9223372036854774784.0  -- largest double < 2^63
                local at_boundary = 9223372036854775808.0  -- exactly 2^63
                
                local under_ok = pcall(function() string.format('%d', just_under) end)
                local boundary_fail = not pcall(function() string.format('%d', at_boundary) end)
                
                return under_ok, boundary_fail
