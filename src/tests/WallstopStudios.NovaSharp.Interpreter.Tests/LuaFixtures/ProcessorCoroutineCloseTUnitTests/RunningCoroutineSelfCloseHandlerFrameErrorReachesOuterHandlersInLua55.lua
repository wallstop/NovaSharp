-- @lua-versions: 5.5
-- @novasharp-only: false
-- @expects-error: false
-- Compatibility notes: Lua 5.5 passes a drained handler-frame error to outer closers.
local log = {}
local task
task = coroutine.create(function()
    local outer <close> = setmetatable({}, {__close = function(_, err)
        log[#log + 1] = 'outer:' .. tostring(err)
    end})
    local b <close> = setmetatable({}, {__close = function(_, err)
        log[#log + 1] = 'b:' .. tostring(err)
        local bLocal <close> = setmetatable({}, {__close = function(_, e)
            log[#log + 1] = 'bLocal:' .. tostring(e)
        end})
        local function inner()
            local iLocal <close> = setmetatable({}, {__close = function(_, e)
                log[#log + 1] = 'iLocal:' .. tostring(e)
                error('i-boom', 0)
            end})
            local function deepest()
                local dLocal <close> = setmetatable({}, {__close = function(_, e)
                    log[#log + 1] = 'dLocal:' .. tostring(e)
                end})
                coroutine.close(task)
                log[#log + 1] = 'd-after'
            end
            deepest()
            log[#log + 1] = 'i-after'
        end
        inner()
        log[#log + 1] = 'b-after'
    end})
    log[#log + 1] = 'body-end'
end)
local resumed, resumeValue = coroutine.resume(task)
local statusAfter = coroutine.status(task)
local closed, closeError = coroutine.close(task)
print(resumed, tostring(resumeValue), statusAfter, closed, tostring(closeError), #log, table.concat(log, ','))
return resumed, tostring(resumeValue), statusAfter, closed, tostring(closeError), #log, table.concat(log, ',')
