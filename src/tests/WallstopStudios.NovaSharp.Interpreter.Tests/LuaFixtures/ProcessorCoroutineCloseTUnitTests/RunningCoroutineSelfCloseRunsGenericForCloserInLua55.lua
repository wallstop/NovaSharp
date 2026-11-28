-- @lua-versions: 5.5
-- @novasharp-only: false
-- @expects-error: false
-- Compatibility notes: Lua 5.5 aborts a running coroutine closed from a generic-for closer.
local log = {}
local task
task = coroutine.create(function()
    for _ in function() end, nil, nil, setmetatable({}, {__close = function(_, err)
        log[#log + 1] = 'for:' .. tostring(err)
        coroutine.close(task)
        log[#log + 1] = 'after-for-close'
    end}) do
        log[#log + 1] = 'iteration'
    end
    log[#log + 1] = 'after-for'
end)

local resumed, resumeValue = coroutine.resume(task)
local statusAfterResume = coroutine.status(task)
local closed, closeError = coroutine.close(task)
print(resumed, tostring(resumeValue), statusAfterResume, closed, tostring(closeError), #log, table.concat(log, ','))
return resumed, tostring(resumeValue), statusAfterResume, closed, tostring(closeError), #log, table.concat(log, ',')
