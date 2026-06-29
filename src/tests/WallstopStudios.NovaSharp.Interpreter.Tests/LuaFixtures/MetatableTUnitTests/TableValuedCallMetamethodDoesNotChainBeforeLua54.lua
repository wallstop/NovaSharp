-- @lua-versions: 5.1-5.3
-- @novasharp-only: false
-- @expects-error: true
-- @source: src/tests/WallstopStudios.NovaSharp.Interpreter.Tests.TUnit/Units/Execution/MetatableTUnitTests.cs
-- @test: MetatableTUnitTests.TableValuedCallMetamethodDoesNotChainBeforeLua54

local target = {}
local proxy = {}

setmetatable(target, { __call = proxy })
setmetatable(proxy, {
    __call = function()
        return "unexpected"
    end,
})

return target()
