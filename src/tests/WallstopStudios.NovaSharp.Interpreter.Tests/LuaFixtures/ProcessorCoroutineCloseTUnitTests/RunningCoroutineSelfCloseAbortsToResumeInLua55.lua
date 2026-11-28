-- @lua-versions: 5.5
-- @novasharp-only: false
-- @expects-error: false
-- Compatibility notes: Lua 5.5 aborts a running coroutine closed from its own execution.
local log = {}
local task
task = coroutine.create(function()
    local outer <close> = setmetatable({}, {__close = function(_, err)
        log[#log + 1] = 'outer:' .. tostring(err)
    end})
    do
        local inner <close> = setmetatable({}, {__close = function(_, err)
            log[#log + 1] = 'inner:' .. tostring(err)
        end})
        local trigger <close> = setmetatable({}, {__close = function(_, err)
            log[#log + 1] = 'trigger:' .. tostring(err)
            local handlerLocal <close> = setmetatable({}, {__close = function(_, e)
                log[#log + 1] = 'handler-local:' .. tostring(e)
            end})
            local ok, closeError = coroutine.close(task)
            log[#log + 1] = 'returned:' .. tostring(ok) .. ':' .. tostring(closeError)
        end})
    end
    log[#log + 1] = 'after-block'
end)

local resumed, resumeValue = coroutine.resume(task)
local statusAfterResume = coroutine.status(task)
local closed, closeError = coroutine.close(task)
local finalStatus = coroutine.status(task)
print(resumed, tostring(resumeValue), statusAfterResume, closed, tostring(closeError), finalStatus, #log, table.concat(log, ','))
return resumed, tostring(resumeValue), statusAfterResume, closed, tostring(closeError), finalStatus, #log, table.concat(log, ','), task
