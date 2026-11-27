-- @lua-versions: all
-- @novasharp-only: true
-- @expects-error: false
-- Uses injected variable: replacement
local task = coroutine.create(function()
    return (string.gsub('abc', 'b', function()
        return replacement()
    end))
end)
local resumed, value = coroutine.resume(task)
local status = coroutine.status(task)

local closed, closeError
if coroutine.close ~= nil then
    closed, closeError = coroutine.close(task)
end

return resumed, value, status, closed, closeError, task
