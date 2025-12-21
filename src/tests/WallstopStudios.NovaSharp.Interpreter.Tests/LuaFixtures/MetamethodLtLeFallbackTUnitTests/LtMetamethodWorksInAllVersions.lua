-- @lua-versions: 5.1, 5.5
-- @novasharp-only: false
-- @expects-error: false
-- @source: src/tests/WallstopStudios.NovaSharp.Interpreter.Tests.TUnit/Spec/MetamethodLtLeFallbackTUnitTests.cs:137
-- @test: MetamethodLtLeFallbackTUnitTests.LtMetamethodWorksInAllVersions
-- @compat-notes: Test targets Lua 5.1
local mt = {
                    __lt = function(a, b) return a.value < b.value end
                    -- __le intentionally NOT defined
                }
                local a = setmetatable({value = 1}, mt)
                local b = setmetatable({value = 2}, mt)
                return a < b  -- Using < operator, which uses __lt directly
