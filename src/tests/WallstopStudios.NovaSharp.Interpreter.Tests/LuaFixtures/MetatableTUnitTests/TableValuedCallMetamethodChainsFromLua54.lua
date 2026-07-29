-- @lua-versions: 5.4+
-- @novasharp-only: false
-- @expects-error: false
-- @source: src/tests/WallstopStudios.NovaSharp.Interpreter.Tests.TUnit/Units/Execution/MetatableTUnitTests.cs
-- @test: MetatableTUnitTests.TableValuedCallMetamethodChainsFromLua54

local target = {}
local proxy = {}

setmetatable(target, { __call = proxy })
setmetatable(proxy, {
    __call = function(...)
        local a, b, c = ...
        assert(select("#", ...) == 2)
        assert(a == proxy)
        assert(b == target)
        assert(c == nil)
        print("PASS")
    end,
})

return target()
