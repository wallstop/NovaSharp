-- @lua-versions: 5.4, 5.5
-- @novasharp-only: false
-- @expects-error: false
-- Compatibility notes: Lua 5.4+ rejects closing an ancestor coroutine with a normal-coroutine error.
local outer
local inner = coroutine.create(function()
    coroutine.close(outer)
    return 'unreachable'
end)
outer = coroutine.create(function()
    return coroutine.resume(inner)
end)
local resumed, innerResumed, message = coroutine.resume(outer)
local rejected = type(message) == 'string'
    and message:find('cannot close a normal coroutine', 1, true) ~= nil
print(resumed, innerResumed, rejected, coroutine.status(inner), coroutine.status(outer))
return resumed, innerResumed, rejected, coroutine.status(inner), coroutine.status(outer)
