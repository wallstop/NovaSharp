-- @lua-versions: all
-- @novasharp-only: true
-- @expects-error: false
-- Uses injected variable: worker
local tail = coroutine.create(function()
    return worker()
end)
local tailResumed, tailValue = coroutine.resume(tail)
local tailStatus = coroutine.status(tail)

local nonTail = coroutine.create(function()
    local value = worker()
    return value
end)
local nonTailResumed, nonTailValue = coroutine.resume(nonTail)
local nonTailStatus = coroutine.status(nonTail)

local tailClosed, tailCloseError, nonTailClosed, nonTailCloseError
if coroutine.close ~= nil then
    tailClosed, tailCloseError = coroutine.close(tail)
    nonTailClosed, nonTailCloseError = coroutine.close(nonTail)
end

return tailResumed, tailValue, tailStatus,
    nonTailResumed, nonTailValue, nonTailStatus,
    tailClosed, tailCloseError, nonTailClosed, nonTailCloseError,
    tail, nonTail
