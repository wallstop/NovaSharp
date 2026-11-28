-- @lua-versions: 5.5
-- @novasharp-only: false
-- @expects-error: false
-- Compatibility notes: A later self-close resets a failure raised while unwinding a popped frame.
local log = {}
local task
task = coroutine.create(function()
    pcall(function()
        local c <close> = setmetatable({}, {__close = function(_, e)
            log[#log + 1] = 'c:' .. tostring(e)
            coroutine.close(task)
        end})
        do
            local a <close> = setmetatable({}, {__close = function(_, e)
                log[#log + 1] = 'a:' .. tostring(e)
                error('a-boom', 0)
            end})
            local b <close> = setmetatable({}, {__close = function(_, e)
                log[#log + 1] = 'b:' .. tostring(e)
                coroutine.close(task)
            end})
            error('boom', 0)
        end
    end)
end)
local resumed, resumeValue = coroutine.resume(task)
local statusAfter = coroutine.status(task)
local closed, closeError = coroutine.close(task)
print(resumed, tostring(resumeValue), statusAfter, closed, tostring(closeError), #log, table.concat(log, ','))
return resumed, tostring(resumeValue), statusAfter, closed, tostring(closeError), #log, table.concat(log, ',')
