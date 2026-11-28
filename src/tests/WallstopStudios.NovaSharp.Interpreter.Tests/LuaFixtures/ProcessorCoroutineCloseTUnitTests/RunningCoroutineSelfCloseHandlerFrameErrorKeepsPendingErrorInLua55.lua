-- @lua-versions: 5.5
-- @novasharp-only: false
-- @expects-error: false
-- Compatibility notes: Lua 5.5 keeps the pending resume error when a handler-local closer fails.
local log = {}
local task
task = coroutine.create(function()
    local a <close> = setmetatable({}, {__close = function(_, err)
        log[#log + 1] = 'a:' .. tostring(err)
    end})
    local b <close> = setmetatable({}, {__close = function(_, err)
        log[#log + 1] = 'b:' .. tostring(err)
        local inner <close> = setmetatable({}, {__close = function(_, e)
            log[#log + 1] = 'inner:' .. tostring(e)
            error('e-boom', 0)
        end})
        coroutine.close(task)
        log[#log + 1] = 'b-after'
    end})
    local c <close> = setmetatable({}, {__close = function(_, err)
        log[#log + 1] = 'c:' .. tostring(err)
        error('c-boom', 0)
    end})
    log[#log + 1] = 'body-end'
end)
local resumed, resumeValue = coroutine.resume(task)
local statusAfterResume = coroutine.status(task)
local closed, closeError = coroutine.close(task)
print(resumed, tostring(resumeValue), statusAfterResume, closed, tostring(closeError), #log, table.concat(log, ','))
return resumed, tostring(resumeValue), statusAfterResume, closed, tostring(closeError), #log, table.concat(log, ',')
