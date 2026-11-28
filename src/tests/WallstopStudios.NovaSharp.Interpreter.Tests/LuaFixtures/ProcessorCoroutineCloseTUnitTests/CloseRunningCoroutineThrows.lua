-- @lua-versions: 5.4
-- @novasharp-only: false
-- @expects-error: false
-- Compatibility notes: Lua 5.4 rejects closing the running coroutine and defers the remaining closer.
local log = {}
local task
task = coroutine.create(function()
    local a <close> = setmetatable({}, {__close = function(_, err)
        log[#log + 1] = 'a:' .. tostring(err ~= nil)
    end})
    local b <close> = setmetatable({}, {__close = function(_, err)
        log[#log + 1] = 'b:' .. tostring(err ~= nil)
        coroutine.close(task)
        log[#log + 1] = 'b-after'
    end})
    log[#log + 1] = 'body-end'
end)

local resumed, resumeError = coroutine.resume(task)
local resumeRejected = type(resumeError) == 'string'
    and resumeError:find('cannot close a running coroutine', 1, true) ~= nil
local statusAfterResume = coroutine.status(task)
local closed, closeError = coroutine.close(task)
local closeRejected = type(closeError) == 'string'
    and closeError:find('cannot close a running coroutine', 1, true) ~= nil
print(resumed, resumeRejected, statusAfterResume, closed, closeRejected, #log, table.concat(log, ','))
return resumed, resumeRejected, statusAfterResume, closed, closeRejected, #log, table.concat(log, ',')
