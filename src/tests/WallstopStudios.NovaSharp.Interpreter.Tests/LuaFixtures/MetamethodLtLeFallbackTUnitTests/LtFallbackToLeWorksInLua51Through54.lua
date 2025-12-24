-- @lua-versions: 5.1, 5.2, 5.3, 5.4
-- @novasharp-only: false
-- @expects-error: false
-- @source: src/tests/WallstopStudios.NovaSharp.Interpreter.Tests.TUnit/Spec/MetamethodLtLeFallbackTUnitTests.cs:53
-- @test: MetamethodLtLeFallbackTUnitTests.LtFallbackToLeWorksInLua51Through54
-- @compat-notes: Test targets Lua 5.1
local mt = {
                __lt = function(a, b) return a.value < b.value end
                -- Note: __le is intentionally NOT defined
            }
            local a = setmetatable({value = 1}, mt)
            local b = setmetatable({value = 2}, mt)
            return a <= b  -- This uses __lt fallback in Lua 5.1-5.4
