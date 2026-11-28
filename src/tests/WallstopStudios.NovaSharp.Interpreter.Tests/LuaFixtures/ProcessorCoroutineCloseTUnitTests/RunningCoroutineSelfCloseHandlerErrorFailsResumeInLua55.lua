-- @lua-versions: 5.5
-- @novasharp-only: false
-- @expects-error: false
-- Compatibility notes: Lua 5.5 reports a closer failure raised while a running coroutine self-closes.
local log = {}
local task
task = coroutine.create(function()
    local a <close> = setmetatable({}, {__close = function(_, err)
        log[#log + 1] = 'a:' .. tostring(err)
    end})
    local b <close> = setmetatable({}, {__close = function(_, err)
        log[#log + 1] = 'b:' .. tostring(err)
        error('b-boom', 0)
    end})
    local c <close> = setmetatable({}, {__close = function(_, err)
        log[#log + 1] = 'c:' .. tostring(err)
        coroutine.close(task)
        log[#log + 1] = 'c-after'
    end})
    log[#log + 1] = 'body-end'
end)

local resumed, resumeValue = coroutine.resume(task)
local statusAfterResume = coroutine.status(task)
local closed, closeError = coroutine.close(task)
print(resumed, tostring(resumeValue), statusAfterResume, closed, tostring(closeError), #log, table.concat(log, ','))
return resumed, tostring(resumeValue), statusAfterResume, closed, tostring(closeError), #log, table.concat(log, ',')
