-- @lua-versions: 5.1+
-- @novasharp-only: false
-- @expects-error: false
-- @source: src/tests/WallstopStudios.NovaSharp.Interpreter.Tests.TUnit/Spec/MetamethodLtLeFallbackTUnitTests.cs:101
-- @test: MetamethodLtLeFallbackTUnitTests.LeMetamethodWorksInAllVersions
local mt = {
                    __lt = function(a, b) return a.value < b.value end,
                    __le = function(a, b) return a.value <= b.value end
                }
                local a = setmetatable({value = 1}, mt)
                local b = setmetatable({value = 2}, mt)
                return a <= b
