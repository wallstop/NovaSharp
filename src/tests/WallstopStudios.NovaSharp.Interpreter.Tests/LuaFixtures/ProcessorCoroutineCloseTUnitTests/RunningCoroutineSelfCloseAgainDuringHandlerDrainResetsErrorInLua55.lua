-- @lua-versions: 5.5
-- @novasharp-only: false
-- @expects-error: false
-- Compatibility notes: Lua 5.5 resets the active error when a closer self-closes again.
local log = {}
local task
task = coroutine.create(function()
    local a <close> = setmetatable({}, {__close = function(_, err)
        log[#log + 1] = 'a:' .. tostring(err)
    end})
    local b <close> = setmetatable({}, {__close = function(_, err)
        log[#log + 1] = 'b:' .. tostring(err)
        local i1 <close> = setmetatable({}, {__close = function(_, e)
            log[#log + 1] = 'i1:' .. tostring(e)
            coroutine.close(task)
            log[#log + 1] = 'i1-after'
        end})
        local i2 <close> = setmetatable({}, {__close = function(_, e)
            log[#log + 1] = 'i2:' .. tostring(e)
            error('i2-boom', 0)
        end})
        coroutine.close(task)
        log[#log + 1] = 'b-after'
    end})
    log[#log + 1] = 'body-end'
end)
local resumed, resumeValue = coroutine.resume(task)
local statusAfter = coroutine.status(task)
local closed, closeError = coroutine.close(task)
print(resumed, tostring(resumeValue), statusAfter, closed, tostring(closeError), #log, table.concat(log, ','))
return resumed, tostring(resumeValue), statusAfter, closed, tostring(closeError), #log, table.concat(log, ',')
