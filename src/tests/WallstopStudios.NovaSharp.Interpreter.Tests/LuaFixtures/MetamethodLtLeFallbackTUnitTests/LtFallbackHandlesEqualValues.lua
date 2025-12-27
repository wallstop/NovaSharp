-- @lua-versions: 5.4, 5.5
-- @novasharp-only: false
-- @expects-error: false
-- @source: src/tests/WallstopStudios.NovaSharp.Interpreter.Tests.TUnit/Spec/MetamethodLtLeFallbackTUnitTests.cs:173
-- @test: MetamethodLtLeFallbackTUnitTests.LtFallbackHandlesEqualValues
-- @compat-notes: Test targets Lua 5.4+
local mt = {
                    __lt = function(a, b) return a.value < b.value end
                    -- __le intentionally NOT defined
                }
                local a = setmetatable({value = 2}, mt)
                local b = setmetatable({value = 2}, mt)
                return a <= b  -- Should be true: not (2 < 2) = not false = true
