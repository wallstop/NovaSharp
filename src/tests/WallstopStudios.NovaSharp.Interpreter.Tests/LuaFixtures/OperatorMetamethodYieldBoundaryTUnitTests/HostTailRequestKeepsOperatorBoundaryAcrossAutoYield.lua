-- @lua-versions: all
-- @novasharp-only: true
-- @expects-error: false

local is51 = _VERSION == "Lua 5.1"
local function hook()
    markHookEntered()
    return hostYield()
end
local operand = setmetatable({}, { __add = makeHostTailRequest })
return hook, function()
    local function evaluate()
        return operand + operand
    end
    local function check(sentinel, ok, value)
        assert(sentinel == "sentinel")
        if is51 then
            assert(not ok and value:find("attempt to yield across metamethod/C-call boundary", 1, true), value)
        else
            assert(ok and value == 42, value)
        end
    end
    check("sentinel", pcall(evaluate))
    return coroutine.yield("recovered")
end
