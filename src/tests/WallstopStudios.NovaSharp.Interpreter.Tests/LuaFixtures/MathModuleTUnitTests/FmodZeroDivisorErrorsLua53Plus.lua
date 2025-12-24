-- Tests that math.fmod(x, 0) throws an error in Lua 5.3+
-- Lua 5.1/5.2 returned NaN instead

-- @lua-versions: 5.3, 5.4, 5.5
-- @novasharp-only: false
-- @expects-error: true
-- @source: src/tests/WallstopStudios.NovaSharp.Interpreter.Tests.TUnit/Modules/MathModuleTUnitTests.cs:0
-- @test: MathModuleTUnitTests.FmodZeroDivisorErrorsLua53Plus
-- @compat-notes: Lua 5.3+ throws error for zero divisor; Lua 5.1/5.2 return NaN

-- This should throw an error in Lua 5.3+
local result = math.fmod(5, 0)

-- Should not reach here
print("ERROR: Should have thrown an error for zero divisor")
