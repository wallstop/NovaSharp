-- @lua-versions: 5.5
-- @novasharp-only: false
-- @expects-error: false
-- Compatibility notes: Lua 5.5 keeps a failure raised after a nested self-close reset.
local log = {}
local task
task = coroutine.create(function()
    local outer <close> = setmetatable({}, {__close = function(_, err)
        log[#log + 1] = 'outer:' .. tostring(err)
    end})
    local b <close> = setmetatable({}, {__close = function(_, err)
        log[#log + 1] = 'b:' .. tostring(err)
        local a <close> = setmetatable({}, {__close = function(_, e)
            log[#log + 1] = 'a:' .. tostring(e)
            error('a-boom', 0)
        end})
        local c <close> = setmetatable({}, {__close = function(_, e)
            log[#log + 1] = 'c:' .. tostring(e)
            coroutine.close(task)
            log[#log + 1] = 'c-after'
        end})
        coroutine.close(task)
    end})
    log[#log + 1] = 'body-end'
end)
local resumed, resumeValue = coroutine.resume(task)
local statusAfter = coroutine.status(task)
local closed, closeError = coroutine.close(task)
print(resumed, tostring(resumeValue), statusAfter, closed, tostring(closeError), #log, table.concat(log, ','))
return resumed, tostring(resumeValue), statusAfter, closed, tostring(closeError), #log, table.concat(log, ',')
