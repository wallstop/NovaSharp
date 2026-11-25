-- @lua-versions: all
-- @novasharp-only: true
-- @expects-error: false

local value = setmetatable({}, { __unm = forwardForcedYield })
local function checkForwarding(sentinel, ok, err)
    assert(sentinel == "sentinel")
    assert(not ok and err:find("cannot forward a forced coroutine yield", 1, true), err)
end
return function()
    checkForwarding("sentinel", pcall(function() return -value end))
    checkForwarding("sentinel", pcall(function() return forwardForcedYield() end))
    checkForwarding("sentinel", pcall(tailForwardForcedYield))
    checkForwarding("sentinel", pcall(function()
        local ok, marker = coroutine.resume(autoYieldChild)
        assert(ok)
        return marker
    end))
    return coroutine.yield("ready")
end
