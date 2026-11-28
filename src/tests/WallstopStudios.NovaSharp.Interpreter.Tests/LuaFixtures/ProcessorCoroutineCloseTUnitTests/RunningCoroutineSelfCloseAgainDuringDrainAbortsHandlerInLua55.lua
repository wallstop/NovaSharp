-- @lua-versions: 5.5
-- @novasharp-only: false
-- @expects-error: false
-- Compatibility notes: Lua 5.5 drains the remaining handlers when a closer self-closes again.
local log = {}
local task
task = coroutine.create(function()
    local outer <close> = setmetatable({}, {__close = function(_, err)
        log[#log + 1] = 'outer:' .. tostring(err)
    end})
    do
        local a <close> = setmetatable({}, {__close = function(_, err)
            log[#log + 1] = 'a:' .. tostring(err)
            coroutine.close(task)
            log[#log + 1] = 'a-after'
        end})
        local b <close> = setmetatable({}, {__close = function(_, err)
            log[#log + 1] = 'b:' .. tostring(err)
            coroutine.close(task)
            log[#log + 1] = 'b-after'
        end})
    end
    log[#log + 1] = 'after-block'
end)

local resumed, resumeValue = coroutine.resume(task)
local statusAfterResume = coroutine.status(task)
local closed, closeError = coroutine.close(task)
print(resumed, tostring(resumeValue), statusAfterResume, closed, tostring(closeError), #log, table.concat(log, ','))
return resumed, tostring(resumeValue), statusAfterResume, closed, tostring(closeError), #log, table.concat(log, ',')
