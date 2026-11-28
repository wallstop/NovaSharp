-- @lua-versions: 5.5
-- @novasharp-only: false
-- @expects-error: false
-- Compatibility notes: Lua 5.5 closes every block of a popped frame before aborting.
local log = {}
local task
task = coroutine.create(function()
    local ok, err = pcall(function()
        local a <close> = setmetatable({}, {__close = function(_, e)
            log[#log + 1] = 'a:' .. tostring(e)
        end})
        do
            local b <close> = setmetatable({}, {__close = function(_, e)
                log[#log + 1] = 'b:' .. tostring(e)
                coroutine.close(task)
                log[#log + 1] = 'b-after'
            end})
            error('boom', 0)
        end
    end)
    log[#log + 1] = 'pcall:' .. tostring(ok) .. ':' .. tostring(err)
end)
local resumed, resumeValue = coroutine.resume(task)
local statusAfter = coroutine.status(task)
local closed, closeError = coroutine.close(task)
print(resumed, tostring(resumeValue), statusAfter, closed, tostring(closeError), #log, table.concat(log, ','))
return resumed, tostring(resumeValue), statusAfter, closed, tostring(closeError), #log, table.concat(log, ',')
