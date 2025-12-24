-- @lua-versions: 5.1, 5.2, 5.3, 5.4, 5.5
-- @novasharp-only: false
-- @expects-error: false
-- @source: src/tests/WallstopStudios.NovaSharp.Interpreter.Tests.TUnit/Spec/MetamethodLtLeFallbackTUnitTests.cs:109
-- @test: MetamethodLtLeFallbackTUnitTests.LeMetamethodWorksInAllVersions
-- @compat-notes: Test targets Lua 5.1
local mt = {
                    __lt = function(a, b) return a.value < b.value end,
                    __le = function(a, b) return a.value <= b.value end
                }
                local a = setmetatable({value = 1}, mt)
                local b = setmetatable({value = 2}, mt)
                return a <= b
